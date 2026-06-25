using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game2DWaterKit
{
	public class GameController : MonoBehaviour
	{
		Rigidbody2D pickedGameObject;
		public float MoveSpeed = 0f;

		LayerMask raycastMask = -1 & ~(1 << 4);

		public Vector3 GetMousePosition()
		{
#if ENABLE_INPUT_SYSTEM
			if (Mouse.current != null)
				return Mouse.current.position.ReadValue();
			else
				return Input.mousePosition; // Fallback to legacy
#else
        return Input.mousePosition;
#endif
		}

		public bool GetMouseButtonDown(int button)
		{
#if ENABLE_INPUT_SYSTEM
			if (Mouse.current != null)
			{
				switch (button)
				{
					case 0: return Mouse.current.leftButton.wasPressedThisFrame;
					case 1: return Mouse.current.rightButton.wasPressedThisFrame;
					case 2: return Mouse.current.middleButton.wasPressedThisFrame;
					default: return false;
				}
			}
			else
				return Input.GetMouseButtonDown(button); // Fallback to legacy
#else
    return Input.GetMouseButtonDown(button);
#endif
		}

		public bool GetMouseButtonUp(int button)
		{
#if ENABLE_INPUT_SYSTEM
			if (Mouse.current != null)
			{
				switch (button)
				{
					case 0: return Mouse.current.leftButton.wasReleasedThisFrame;
					case 1: return Mouse.current.rightButton.wasReleasedThisFrame;
					case 2: return Mouse.current.middleButton.wasReleasedThisFrame;
					default: return false;
				}
			}
			else
				return Input.GetMouseButtonUp(button); // Fallback to legacy
#else
    return Input.GetMouseButtonUp(button);
#endif
		}

		public float GetHorizontalAxis()
		{
#if ENABLE_INPUT_SYSTEM
			if (Keyboard.current != null)
			{
				float value = 0f;
				if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
					value -= 1f;
				if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
					value += 1f;
				return value;
			}
			else
				return Input.GetAxis("Horizontal"); // Fallback to legacy
#else
    return Input.GetAxis("Horizontal");
#endif
		}

		public float GetVerticalAxis()
		{
#if ENABLE_INPUT_SYSTEM
			if (Keyboard.current != null)
			{
				float value = 0f;
				if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
					value += 1f;
				if (Keyboard.current.zKey.isPressed || Keyboard.current.downArrowKey.isPressed)
					value -= 1f;
				return value;
			}
			else
				return Input.GetAxis("Vertical"); // Fallback to legacy
#else
    return Input.GetAxis("Vertical");
#endif
		}

		void Update()
		{
			Camera mainCam = Camera.main;
			Vector2 mousePos = mainCam.ScreenToWorldPoint(GetMousePosition());

			if (GetMouseButtonDown(0))
			{
				RaycastHit2D hit = Physics2D.Raycast(mousePos, mainCam.transform.forward, float.MaxValue, raycastMask);
				if (hit.rigidbody != null && hit.rigidbody.CompareTag("Pickable"))
				{
					pickedGameObject = hit.rigidbody;
#if UNITY_6000_0_OR_NEWER
					pickedGameObject.bodyType = RigidbodyType2D.Kinematic;
#else
					pickedGameObject.isKinematic = true;
#endif
				}
			}

			if (pickedGameObject && GetMouseButtonUp(0))
			{
#if UNITY_6000_0_OR_NEWER
				pickedGameObject.bodyType = RigidbodyType2D.Dynamic;
#else
				pickedGameObject.isKinematic = false;
#endif
				pickedGameObject = null;
			}

			if (pickedGameObject)
			{
				pickedGameObject.MovePosition(mousePos);
			}

			if (MoveSpeed > 0f)
			{
				Vector3 camPos = mainCam.transform.position;
				float moveSpeed = Time.deltaTime * MoveSpeed;
				camPos.x += GetHorizontalAxis() * moveSpeed;
				camPos.y += GetVerticalAxis() * moveSpeed;
				mainCam.transform.position = camPos;
			}
		}
	}
}
