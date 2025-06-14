﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRPLearn{
    public class RenderObjectPass
    {
        
        private ShaderTagId _shaderTag;
        private bool _isTransparent = false;
        private bool _perObjectLight = false;

        public RenderObjectPass(bool transparent,string lightModeTagId,bool perObjectLight){
            _shaderTag = new ShaderTagId(lightModeTagId);
            _isTransparent = transparent;
            _perObjectLight = perObjectLight;
        }

        public RenderObjectPass(bool transparent,string lightModeTagId):this(transparent,lightModeTagId,true){

        }

        public RenderObjectPass(bool transparent):this(transparent,"XForwardBase"){
        }
        
        public void Execute(ScriptableRenderContext context, Camera camera,ref CullingResults cullingResults){
            var drawSetting = CreateDrawSettings(camera);
            //根据_isTransparent，利用RenderQueueRange来过滤出透明物体，或者非透明物体
            var filterSetting = new FilteringSettings(_isTransparent? RenderQueueRange.transparent:RenderQueueRange.opaque);
            //绘制物体
            context.DrawRenderers(cullingResults,ref drawSetting,ref filterSetting);
            //draw skybox
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
            {
                context.DrawSkybox(camera);
            }            
        }

        private DrawingSettings CreateDrawSettings(Camera camera){
            var sortingSetting = new SortingSettings(camera);
            //设置物体渲染排序标准
            sortingSetting.criteria = _isTransparent?SortingCriteria.CommonTransparent:SortingCriteria.CommonOpaque;
            var drawSetting = new DrawingSettings(_shaderTag,sortingSetting);
            drawSetting.perObjectData |= PerObjectData.LightData;
            drawSetting.perObjectData |= PerObjectData.LightIndices;
            return drawSetting;
        }
    }
}
