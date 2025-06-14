using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace SRPLearn
{
    public class BlurPass
    {
        private static readonly int _BlurBlitTex = Shader.PropertyToID("_BlurBlitTex");
        private Material _blurMaterial;
        private RenderTargetIdentifier _source;
        private RenderTargetIdentifier _target1;
        private RenderTargetIdentifier _target2;
        private CommandBuffer _commandBuffer;

        private const int DownSamplePassId = 0;
        private const int BlurHorizontalPassId = 1;
        private const int BlurVerticalPassId = 2;

        private float _softBlurDownSample;
        private float _heavyBlurDownSample;
        
        private List<int> _tempRTIds = new List<int>();
        
        public BlurPass()
        {
            _commandBuffer = new CommandBuffer();
            _commandBuffer.name = "Blur";
        }

        public void Config(RenderTargetIdentifier source, RenderTargetIdentifier target1,RenderTargetIdentifier target2,ref AOSASetting setting)
        {
            _source = source;
            _target1 = target1;
            _target2 = target2;
            _softBlurDownSample = setting.softBlurDownsample;
            _heavyBlurDownSample = setting.heavyBlurDownsample;
        }

        public void Execute(ScriptableRenderContext context, Camera camera)
        {
            if (!_blurMaterial)
            {
                _blurMaterial = new Material(Shader.Find("Hidden/SRPLearn/AOSABlur"));
            }

            _commandBuffer.Clear();
            DoBlur(_source, camera);
            context.ExecuteCommandBuffer(_commandBuffer);
        }

        private void Draw(RenderTargetIdentifier source, RenderTargetIdentifier target, int passId)
        {
            _commandBuffer.SetGlobalTexture(_BlurBlitTex, source);
            _commandBuffer.Blit(source, target, _blurMaterial, passId);
        }

        private void DoBlur(RenderTargetIdentifier source, Camera camera)
        {
            int width = camera.pixelWidth;
            int height = camera.pixelHeight;

            // -------------------- Soft Blur --------------------
            int softW = Mathf.CeilToInt(width / _softBlurDownSample);
            int softH = Mathf.CeilToInt(height / _softBlurDownSample);

            int softRT1 = Shader.PropertyToID("_SoftBlurRT1");
            int softRT2 = Shader.PropertyToID("_SoftBlurRT2");

            _commandBuffer.GetTemporaryRT(softRT1, softW, softH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            _commandBuffer.GetTemporaryRT(softRT2, softW, softH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            _tempRTIds.Add(softRT1);
            _tempRTIds.Add(softRT2);

            // Downsample from source
            Draw(source, softRT1, DownSamplePassId);

            // Blur pass: horizontal + vertical
            Draw(softRT1, softRT2, BlurHorizontalPassId);
            Draw(softRT2, _target1, BlurVerticalPassId); // Output soft blur to _target1


            // -------------------- Heavy Blur --------------------
            int heavyW = Mathf.CeilToInt(width / _heavyBlurDownSample);
            int heavyH = Mathf.CeilToInt(height / _heavyBlurDownSample);

            int heavyRT1 = Shader.PropertyToID("_HeavyBlurRT1");
            int heavyRT2 = Shader.PropertyToID("_HeavyBlurRT2");

            _commandBuffer.GetTemporaryRT(heavyRT1, heavyW, heavyH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            _commandBuffer.GetTemporaryRT(heavyRT2, heavyW, heavyH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            _tempRTIds.Add(heavyRT1);
            _tempRTIds.Add(heavyRT2);

            // Downsample from soft blurred result
            Draw(_target1, heavyRT1, DownSamplePassId);

            // Blur pass: horizontal + vertical
            Draw(heavyRT1, heavyRT2, BlurHorizontalPassId);
            Draw(heavyRT2, _target2, BlurVerticalPassId); // Output heavy blur to _target2


            // -------------------- 清理临时RT --------------------
            foreach (var id in _tempRTIds)
            {
                _commandBuffer.ReleaseTemporaryRT(id);
            }
            _tempRTIds.Clear();
        }

    }
}
