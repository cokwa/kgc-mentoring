using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static PlayerManager localPlayer;

    public float walkingSpeed = 3f;
    public float runningSpeed = 5f;

    public float movementLerp = 0.1f;

    public float cameraRotationSlerp = 0.1f;
    private Quaternion targetCameraRotation = Quaternion.identity;

    public float lookSensitivity = 1f;

    public PhysicMaterial movingPhysMat, standingPhysMat;

    public GameObject head;

    public Collider bodyCollider;

    private Vector3 cameraRotation;

    private Rigidbody myRigidbody;

    private MonitorSync monitor;

    private bool _controlsCamera = true;
    public bool controlsCamera
    {
        get
        {
            return _controlsCamera;
        }

        set
        {
            _controlsCamera = value;

            Cursor.visible = !_controlsCamera;
            Cursor.lockState = _controlsCamera ? CursorLockMode.Confined : CursorLockMode.None;
        }
    }

    public override void OnEnable()
    {
        controlsCamera = true;
    }

    public override void OnDisable()
    {
        controlsCamera = false;
    }

    private void Awake()
    {
        if (photonView.IsMine)
        {
            localPlayer = this;

            Camera.main.transform.parent = head.transform;
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
        }

        myRigidbody = GetComponent<Rigidbody>();

        monitor = FindObjectOfType<MonitorSync>();
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            head.transform.localRotation = Quaternion.Slerp(head.transform.localRotation, targetCameraRotation, cameraRotationSlerp);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            controlsCamera = !controlsCamera;

            if (monitor)
            {
                monitor.uiOpen = !monitor.uiOpen;
            }
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

        if (monitor && !monitor.uiOpen && Input.GetMouseButtonDown(0))
        {
            monitor.uiOpen = true;
        }
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine)
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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(head.transform.localRotation);
        }
        else
        {
            targetCameraRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
