using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch : Cell {

    public MeshRenderer targetLight;
    public MeshRenderer currentLight;

    public Color emissionColor = new Color(0.4f, 1f, 1f);

    [Range(0, 1)]
    public float currentLightValue;
    [Range(0, 1)]
    public float targetLightValue;

    private MaterialPropertyBlock targetMPB;
    private MaterialPropertyBlock currentMPB;

    private int emissionColorHash = Shader.PropertyToID("_EmissionColor");
    
    private void Awake()
    {
        anim = GetComponent<Animator>();

        targetMPB = new MaterialPropertyBlock();
        currentMPB = new MaterialPropertyBlock();
    }

    private void Update()
    {
        UpdateColors();
    }

    private void UpdateColors()
    {
        //Color currentColor = currentMPB.GetColor(emissionColorHash);
        //currentColor.a = currentLightValue;
        //currentMPB.SetColor(emissionColorHash, currentColor);
        currentMPB.SetColor(emissionColorHash, Mathf.Clamp01(currentLightValue) * emissionColor);
        currentLight.SetPropertyBlock(currentMPB);

        //Color targetColor = targetMPB.GetColor(emissionColorHash);
        //targetColor.a = targetLightValue;
        //targetMPB.SetColor(emissionColorHash, targetColor);
        targetMPB.SetColor(emissionColorHash, targetLightValue * emissionColor);
        targetLight.SetPropertyBlock(targetMPB);
    }

    public override void OnCellPlaced()
    {
        targetLightValue = 0f;
        currentLightValue = 0f;
        base.OnCellPlaced();
    }

    public override void OnCellStateChanged(byte state, byte target)
    {
        targetLightValue = target > 0 ? 1f : 0f;
        base.OnCellStateChanged(state, target);
    }
}
