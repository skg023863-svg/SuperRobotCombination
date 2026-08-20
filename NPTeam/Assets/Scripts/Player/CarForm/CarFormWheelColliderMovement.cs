using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarFormWheelColliderMovement : NetworkBehaviour
{
    [SerializeField] Rigidbody carFormRigidBody;
    private NPTeamInputActions _carFormInput;
    
    public float MotorTorque;
    public float BrakeTorque;
    public float SteerRot;
    
    public NetworkVariable<bool> isMove = new NetworkVariable<bool>
        (default, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner);
    
    private NetworkVariable<float> _netHorizontalInput = new NetworkVariable<float>
        (0f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner);
    
    private float _brakeInput;
    
    private float _verticalInput;

    public WheelCollider[] SteerWheel;
    public WheelCollider[] MotorWheel;
    public WheelCollider[] AllWheel;

    [Header("부모 객체인 PlayerVehicle를 참조")]
    [SerializeField] private GameObject _playerVehicle;
    [SerializeField] private PlayerVehicle _playerVehicleCS;

    private float _timer;
    private PlayerStun _stun;

    void Awake()
    {
        carFormRigidBody.centerOfMass = new Vector3(0f, -0.5f, 0f);
        _carFormInput = new NPTeamInputActions();
        _stun = GetComponentInParent<PlayerStun>();
        WheelCenterSetting();
    }
    
    void OnEnable()
    {
        _carFormInput.asset.Enable();
        _carFormInput.Player.PlayerMove.performed += CarFormInput;
        _carFormInput.Player.PlayerMove.canceled += CarFormInput;
        _carFormInput.Player.PlayerAscend.performed += CarFormInputBreak;
        _carFormInput.Player.PlayerAscend.canceled += CarFormInputBreak;
    }

    void OnDisable()
    {
        _carFormInput.Player.PlayerMove.performed -= CarFormInput;
        _carFormInput.Player.PlayerMove.canceled -= CarFormInput;
        _carFormInput.Player.PlayerAscend.performed -= CarFormInputBreak;
        _carFormInput.Player.PlayerAscend.canceled -= CarFormInputBreak;
        _carFormInput.asset.Disable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (_playerVehicleCS.Stamina >= 100) return;

        _timer += Time.deltaTime;
        if (_timer >= 1f)
        {
            _timer = 0f;
            _playerVehicleCS.ChangeStamina(2);
        }
    }

    void FixedUpdate()
    {
        if (_playerVehicleCS.checkSpeedOff == true)
        {
            _netHorizontalInput.Value = 0;
            carFormRigidBody.linearVelocity = Vector3.zero;
            carFormRigidBody.angularVelocity = Vector3.zero;
            _playerVehicleCS.checkSpeedOff = false;
        }
        
        if (IsOwner && !_stun.IsStunned)
        {
            WheelControl();
        }
        
        if (!IsOwner)
        {
            UpdateSteerWheelVisuals();
        }
        
        UpdateAllWheelVisuals();
    }

    void WheelCenterSetting()
    {
        foreach (WheelCollider wheel in AllWheel)
        {
            wheel.center = wheel.transform.GetChild(0).transform.localPosition;
        }
    }
    
    void WheelControl()
    {
        float rot = SteerRot * _netHorizontalInput.Value;
        float Torque = MotorTorque * _verticalInput;
        float Brake = BrakeTorque * _brakeInput;

        foreach (WheelCollider wheel in SteerWheel)
        {
            wheel.steerAngle = rot;
        }

        foreach (WheelCollider wheel in MotorWheel)
        {
            wheel.motorTorque = Torque; 
            wheel.brakeTorque = Brake;
        }
    }
    
    void UpdateSteerWheelVisuals()
    {
        float rot = SteerRot * _netHorizontalInput.Value;
        
        foreach (WheelCollider wheel in SteerWheel) 
        { 
            wheel.steerAngle = rot; 
        }
    }

    void UpdateAllWheelVisuals()
    {
        foreach (WheelCollider wheel in SteerWheel)
        {
            UpdateWheelVisual(wheel.transform.GetChild(0), wheel);
        }

        foreach (WheelCollider wheel in MotorWheel)
        {
            UpdateWheelVisual(wheel.transform.GetChild(0), wheel);
        }
    }

    void UpdateWheelVisual(Transform trans, WheelCollider wheelCol)
    {
        Vector3 UpdatePos;
        Quaternion UpdateRot;
        wheelCol.GetWorldPose(out UpdatePos, out UpdateRot);
        trans.position = UpdatePos;
        trans.rotation = UpdateRot;
    }
    
    void CarFormInput(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;
        if (PlayerState.Instance.IsPossession == false ||
         PlayerState.Instance.CurrentPossessed != _playerVehicle) return;
        Vector2 input = ctx.ReadValue<Vector2>();
        _netHorizontalInput.Value = input.x;
        _verticalInput = input.y;

        isMove.Value = Mathf.Abs(input.y) > 0.01f;
    }

    void CarFormInputBreak(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;
        if (PlayerState.Instance.IsPossession == false ||
         PlayerState.Instance.CurrentPossessed != _playerVehicle) return;
        _brakeInput = ctx.ReadValue<float>();
    }
}
