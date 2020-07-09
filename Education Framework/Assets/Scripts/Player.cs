using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
    public static Player localPlayer;

    public float walkingSpeed = 3f;
    public float runningSpeed = 5f;

    public float movementLerp = 0.1f;

    public float lookSensitivity = 1f;

    public PhysicMaterial movingPhysMat, standingPhysMat;

    public GameObject head;

    public Collider bodyCollider;

    private Vector3 cameraRotation;

    private Rigidbody myRigidbody;

    private Slides slides;

    public bool controlsCamera
    {
        get
        {
            return !Cursor.visible;
        }

        set
        {
            Cursor.visible = !value;
            Cursor.lockState = value ? CursorLockMode.Confined : CursorLockMode.None;
        }
    }

    public override void OnStartLocalPlayer()
    {
        slides = FindObjectOfType<Slides>();

        if (isServer)
        {
            slides.networkIdentity.AssignClientAuthority(connectionToClient);
        }
    }

    private void OnEnable()
    {
        controlsCamera = true;
    }

    private void OnDisable()
    {
        controlsCamera = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        localPlayer = this;

        if (isLocalPlayer)
        {
            Camera.main.transform.parent = head.transform;
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
        }

        myRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (controlsCamera)
        {
            cameraRotation += new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) * lookSensitivity;

            Quaternion xRotation = Quaternion.AngleAxis(cameraRotation.x, Vector3.right);
            Quaternion yRotation = Quaternion.AngleAxis(cameraRotation.y, Vector3.up);
            Quaternion zRotation = Quaternion.AngleAxis(cameraRotation.z, Vector3.forward);

            transform.localRotation = yRotation;
            head.transform.localRotation = xRotation * zRotation;
        }

        if (!slides.uiOpen && Input.GetMouseButtonDown(0))
        {
            slides.uiOpen = true;
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        bool running = Input.GetKey(KeyCode.LeftShift);
        float speed = running ? runningSpeed : walkingSpeed;

        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            direction += transform.forward;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction -= transform.forward;
        }

        if (Input.GetKey(KeyCode.A))
        {
            direction -= transform.right;
        }

        if (Input.GetKey(KeyCode.D))
        {
            direction += transform.right;
        }

        if (direction.sqrMagnitude > 0f)
        {
            bodyCollider.material = movingPhysMat;
        }
        else
        {
            bodyCollider.material = standingPhysMat;
        }

        direction.Normalize();

        myRigidbody.velocity = Vector3.Lerp(myRigidbody.velocity, direction * speed, movementLerp);
    }
}
