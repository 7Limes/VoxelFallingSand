using UnityEngine;
using UnityEngine.Events;

public class Tool : MonoBehaviour
{
    [SerializeField] private Transform returnTransform;
    [SerializeField] private float returnDuration = 0.5f;

    [SerializeField] private Rigidbody rb;

    [SerializeField] private UnityEvent activateEvent;

    private bool isTriggerPressed = false;
    private bool isGrabbed = false;
    private float returnTimer = 0;

    void FixedUpdate()
    {
        if (isGrabbed && isTriggerPressed)
        {
            activateEvent.Invoke();
        }
    }

    void Update()
    {
        returnTimer = Mathf.MoveTowards(returnTimer, returnDuration, Time.deltaTime);
        if (isGrabbed)
        {
            returnTimer = 0;
        }
        else if (returnTimer < returnDuration)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            float t = returnTimer / returnDuration;
            transform.position = Vector3.Lerp(transform.position, returnTransform.position, t);
            transform.rotation = Quaternion.Lerp(transform.rotation, returnTransform.rotation, t);
        }
    }

    public void OnTriggerPressed()
    {
        isTriggerPressed = true;
    }

    public void OnTriggerReleased()
    {
        isTriggerPressed = false;
    }

    public void OnGrabbed()
    {
        isGrabbed = true;
    }

    public void OnReleased()
    {
        isGrabbed = false;
    }
}
