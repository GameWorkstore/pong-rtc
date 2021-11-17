using System.Collections.Generic;
using Unity.RenderStreaming;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Pong
{
    public class PongController : MonoBehaviour
    {
        private float MovimentSpeed = 3;
        private readonly List<Keyboard> _keyboards = new List<Keyboard>();

        private void Awake()
        {
            var receiver = FindObjectOfType<InputChannelReceiverBase>();
            receiver.onDeviceChange += OnDeviceChange;

            EnhancedTouchSupport.Enable();
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

        private float GetInput()
        {
            float direction = 0;

            // keyboard control
            foreach (var keyboard in _keyboards)
            {
                if (keyboard.wKey.isPressed)
                {
                    direction += 1;
                }
                if (keyboard.sKey.isPressed)
                {
                    direction += -1;
                }
            }

            return direction;
        }

        private void Update()
        {
            var pos = transform.localPosition.y + GetInput() * MovimentSpeed * Time.deltaTime;
            var clamp = Mathf.Clamp(pos,-4.0f,4.0f);
            transform.localPosition = new Vector3(transform.localPosition.x, clamp, transform.localPosition.z);
        }
    }
}
