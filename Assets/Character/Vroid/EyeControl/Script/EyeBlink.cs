using System;
using UnityEngine;
using UnityEngine.Serialization;
using UniVRM10;

public class EyeBlink : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Vrm10Instance  vrm10Instance;
    private float _lastBlink = 0.0f;
    private float _blinkValue = 0.0f;
    private bool _isBlinking = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _lastBlink += Time.deltaTime;
        if (_lastBlink >= 4.0f)
        {
            _lastBlink = 0.0f;
            _isBlinking = true;
            BlinkOnce();
        }
        else
        {
            BlinkOnce();
        }
        //skinnedMeshRenderer.SetBlendShapeWeight(14,blinkValue);
        //Debug.Log();
        vrm10Instance.Runtime.Expression.SetWeight(ExpressionKey.Blink, _blinkValue);
    }

    private void BlinkOnce()
    {
        if (_isBlinking)
        {
            _blinkValue = FInterpTo(_blinkValue,1,Time.deltaTime,20);
            if(_blinkValue > 0.98f) _isBlinking = false;
        }
        else
        {
            _blinkValue = FInterpTo(_blinkValue,0,Time.deltaTime,20);
        }
        
    }
    
    private static float FInterpTo(float current, float target, float deltaTime, float interpSpeed)
    {
        if (interpSpeed <= 0f)
            return target;

        float dist = target - current;

        // 
        if (dist * dist < 1e-8f)
            return target;

        float deltaMove = dist * Mathf.Clamp01(deltaTime * interpSpeed);
        return current + deltaMove;
    }
}

















