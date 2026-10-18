using UnityEngine;

public class gemanimation : MonoBehaviour
{
    public float rotationSpeed = 65f;
    public float bobAmplitude = 0.25f;
    public float bobFrequency = 2.2f;

    private Vector3 _startLocalPos;
    private bool _initialized;

    void Start()
    {
        _startLocalPos = transform.localPosition;
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized)
        {
            _startLocalPos = transform.localPosition;
            _initialized = true;
        }

        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
        float offsetY = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.localPosition = _startLocalPos + new Vector3(0f, offsetY, 0f);
    }
}
