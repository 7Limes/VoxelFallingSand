using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Potentiometer : MonoBehaviour
{
    [SerializeField] private Transform anchorPoint;

    [SerializeField] private float minAngle = -90;
    [SerializeField] private float maxAngle = 90;

    [SerializeField] private float minValue = 0;
    [SerializeField] private float maxValue = 10;
    [SerializeField] private float defaultValue = 5;

    private XRGrabInteractable grabInteractable;

    private bool isGrabbed = false;
    private float startYRotation = 0;
    private float interactorStartRotation = 0;
    private float currentRotation = 0;

    private float currentValue = 0;

    public float ReadValue()
    {
        return currentValue;
    }

    public void OnGrab()
    {
        isGrabbed = true;
        startYRotation = currentRotation;
        interactorStartRotation = grabInteractable.firstInteractorSelecting.transform.eulerAngles.z;
    }

    public void OnRelease()
    {
        isGrabbed = false;
    }

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Start()
    {
        currentValue = defaultValue;
        currentRotation = MapRange(currentValue, minValue, maxValue, minAngle, maxAngle);
        currentRotation = transform.localEulerAngles.y;
    }

    void Update()
    {
        if (isGrabbed)
        {
            Quaternion targetRotation = grabInteractable.firstInteractorSelecting.transform.rotation;
            float rotationOffset = Mathf.DeltaAngle(interactorStartRotation, targetRotation.eulerAngles.z);

            currentRotation = startYRotation - rotationOffset;
            currentRotation = Mathf.Clamp(currentRotation, minAngle, maxAngle);
            currentValue = MapRange(currentRotation, minAngle, maxAngle, minValue, maxValue);
        }

        transform.position = anchorPoint.position;
        transform.rotation = anchorPoint.rotation * Quaternion.Euler(new Vector3(0, currentRotation, 0));
    }

    private float MapRange(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
    }
}
