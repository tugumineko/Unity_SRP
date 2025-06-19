using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerObjectMaterialProperties : MonoBehaviour
{

    private static MaterialPropertyBlock Block;
    
    [SerializeField]private Color color = Color.white;

    [SerializeField]private Color shadowedColor = Color.black;
    
    [SerializeField][Range(0,1)]private float smoothness = 0.5f;
    
    [SerializeField]private Vector2 worldSpaceUVGradient = Vector2.one;
    
    [SerializeField]private Color overlay = Color.gray;
    
    [SerializeField]private float saturation = 1.0f;
    
    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        Block ??= new ();
        Block.SetColor(ShaderProperties.Color, color);
        Block.SetFloat(ShaderProperties.Smoothness, smoothness);
        Block.SetFloat(ShaderProperties.WorldSpaceUVGradientX, worldSpaceUVGradient.x);
        Block.SetFloat(ShaderProperties.WorldSpaceUVGradientY, worldSpaceUVGradient.y);
        Block.SetColor(ShaderProperties.BaseShadowedColor,shadowedColor);
        Block.SetColor(ShaderProperties.BaseColorOverlay, overlay);
        Block.SetFloat(ShaderProperties.BaseColorSaturation, saturation);
        GetComponent<Renderer>().SetPropertyBlock(Block);
    }

    public static class ShaderProperties
    {
        public static readonly int Color = Shader.PropertyToID("_Color");
        public static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        public static readonly int WorldSpaceUVGradientX = Shader.PropertyToID("_WorldSpaceUVGradientX");
        public static readonly int WorldSpaceUVGradientY = Shader.PropertyToID("_WorldSpaceUVGradientY");
        public static readonly int BaseShadowedColor = Shader.PropertyToID("_BaseShadowedColor");
        public static readonly int BaseColorOverlay = Shader.PropertyToID("_BaseColorOverlay");
        public static readonly int BaseColorSaturation = Shader.PropertyToID("_BaseColorSaturation");
    }
    
}