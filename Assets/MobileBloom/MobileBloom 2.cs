using UnityEngine;
using UnityEngine.XR;

[ExecuteInEditMode]
public class MobileBloom2 : MonoBehaviour
{
    [Range(0, 2)]
    public float _BloomDiffusion = 2f;
    public Color _BloomColor = Color.white;
    [Range(0, 5)]
    public float _BloomAmount = 1f;
    [Range(0, 1)]
    public float _BloomThreshold = 0f;
    [Range(0, 1)]
    public float _BloomSoftness = 0f;

    static readonly int _BlurAmountString = Shader.PropertyToID("_BlurAmount");
    static readonly int _BloomColorString = Shader.PropertyToID("_BloomColor");
    static readonly int _BlDataString = Shader.PropertyToID("_BloomData");
    static readonly int _BloomTexString = Shader.PropertyToID("_BloomTex");

    public Material _Material = null;
    private int _NumberOfPasses;
    private float _Knee;
    RenderTextureDescriptor half, quarter, eighths, sixths;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_BloomDiffusion == 0 && _BloomAmount == 0)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (XRSettings.enabled)
        {
            half = XRSettings.eyeTextureDesc;
            half.height /= 2; half.width /= 2;
            quarter = XRSettings.eyeTextureDesc;
            quarter.height /= 4; quarter.width /= 4;
            eighths = XRSettings.eyeTextureDesc;
            eighths.height /= 8; eighths.width /= 8;
            sixths = XRSettings.eyeTextureDesc;
            sixths.height /= XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass ? 8 : 16; sixths.width /= XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass ? 8 : 16;
        }
        else
        {
            half = new RenderTextureDescriptor(Screen.width / 2, Screen.height / 2);
            quarter = new RenderTextureDescriptor(Screen.width / 4, Screen.height / 4);
            eighths = new RenderTextureDescriptor(Screen.width / 8, Screen.height / 8);
            sixths = new RenderTextureDescriptor(Screen.width / 16, Screen.height / 16);
        }

        _Material.SetFloat(_BlurAmountString, _BloomDiffusion);
        _Material.SetColor(_BloomColorString, _BloomAmount * _BloomColor);
        _Knee = _BloomThreshold * _BloomSoftness;
        _Material.SetVector(_BlDataString, new Vector4(_BloomThreshold, _BloomThreshold - _Knee, 2f * _Knee, 1f / (4f * _Knee + 0.00001f)));
        _NumberOfPasses = Mathf.Clamp(Mathf.CeilToInt(_BloomDiffusion * 4), 1, 4);
        _Material.SetFloat(_BlurAmountString, _NumberOfPasses > 1 ? _BloomDiffusion > 1 ? _BloomDiffusion : (_BloomDiffusion * 4 - Mathf.FloorToInt(_BloomDiffusion * 4 - 0.001f)) * 0.5f + 0.5f : _BloomDiffusion * 4);
        RenderTexture blurTex = null;

        if (_NumberOfPasses == 1 || _BloomDiffusion == 0)
        {
            blurTex = RenderTexture.GetTemporary(half);
            blurTex.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, blurTex, _Material, 0);
        }
        else if (_NumberOfPasses == 2)
        {
            blurTex = RenderTexture.GetTemporary(half);
            var temp1 = RenderTexture.GetTemporary(quarter);
            blurTex.filterMode = FilterMode.Bilinear;
            temp1.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, temp1, _Material, 0);
            Graphics.Blit(temp1, blurTex, _Material, 1);
            RenderTexture.ReleaseTemporary(temp1);
        }
        else if (_NumberOfPasses == 3)
        {
            blurTex = RenderTexture.GetTemporary(quarter);
            var temp1 = RenderTexture.GetTemporary(eighths);
            blurTex.filterMode = FilterMode.Bilinear;
            temp1.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, blurTex, _Material, 0);
            Graphics.Blit(blurTex, temp1, _Material, 1);
            Graphics.Blit(temp1, blurTex, _Material, 1);
            RenderTexture.ReleaseTemporary(temp1);
        }
        else if (_NumberOfPasses == 4)
        {
            blurTex = RenderTexture.GetTemporary(quarter);
            var temp1 = RenderTexture.GetTemporary(eighths);
            var temp2 = RenderTexture.GetTemporary(sixths);
            blurTex.filterMode = FilterMode.Bilinear;
            temp1.filterMode = FilterMode.Bilinear;
            temp2.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, blurTex, _Material, 0);
            Graphics.Blit(blurTex, temp1, _Material, 1);
            Graphics.Blit(temp1, temp2, _Material, 1);
            Graphics.Blit(temp2, temp1, _Material, 1);
            Graphics.Blit(temp1, blurTex, _Material, 1);
            RenderTexture.ReleaseTemporary(temp1);
            RenderTexture.ReleaseTemporary(temp2);
        }
        _Material.SetTexture(_BloomTexString, blurTex);
        RenderTexture.ReleaseTemporary(blurTex);
        Graphics.Blit(source, destination, _Material, 2);
    }
}