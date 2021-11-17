using System;
using System.Collections.Generic;
using Unity.RenderStreaming;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ball : MonoBehaviour
{
    private float Velocity = 0;
    private Vector3 VelocityDir = Vector3.zero;
    private float Radius;
    private Vector3 HitNormal;
    private readonly List<Keyboard> _keyboards = new List<Keyboard>();
    private BoxCollider[] Colliders;

    private void Awake()
    {
        var receiver = FindObjectOfType<InputChannelReceiverBase>();
        receiver.onDeviceChange += OnDeviceChange;
        Radius = transform.localScale.x / 2.0f;
        Colliders = FindObjectsOfType<BoxCollider>();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
                SetDevice(device);
                return;
            case InputDeviceChange.Removed:
                SetDevice(device, false);
                return;
        }
    }

    private void SetDevice(InputDevice device, bool add = true)
    {
        switch (device)
        {
            case Keyboard keyboard:
                if (add)
                    _keyboards.Add(keyboard);
                else
                    _keyboards.Remove(keyboard);
                return;
        }
    }

    private void SetNewVelocity()
    {
        var r = (2 * UnityEngine.Random.value - 1) * 60.0f;
        var q = Quaternion.Euler(0, 0, r);
        Velocity = 3;
        VelocityDir = q * Vector3.right;
        transform.position = Vector3.zero;
    }

    private void Update()
    {
        foreach(var keyboard in _keyboards)
        {
            if (keyboard.escapeKey.isPressed)
            {
                SetNewVelocity();
            }
        }

        var delta = Velocity * Time.deltaTime;
        var pos = transform.localPosition;

        var center = pos + VelocityDir * Radius;

        if(SphereCast(center,Radius, out var hit) && HitNormal != hit.normal)
        {
            delta = delta - (hit.distance - Radius);
            var bounceVec = CalculateBounce(VelocityDir, hit.normal);
            transform.localPosition = hit.point + bounceVec * delta - VelocityDir * Radius;
            VelocityDir = bounceVec;
            HitNormal = hit.normal;
        }
        else
        {
            transform.localPosition = pos + VelocityDir * delta;
        }
    }

    private Vector3 CalculateBounce(Vector3 vector, Vector3 normal)
    {
        return vector - 2 * normal * Vector3.Dot(normal, vector);
    }

    private bool SphereCast(Vector3 center, float radius, out RaycastHit hit)
    {
        foreach(var collider in Colliders)
        {
            var dir = (collider.transform.position - center).normalized;
            if(Physics.Raycast(center, dir, out hit, radius, Physics.AllLayers, QueryTriggerInteraction.UseGlobal))
            {
                return true;
            }
        }
        hit = new RaycastHit();
        return false;
    }
}
