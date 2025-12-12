    using Ilumisoft.SkillDrive.Input;
    using Photon.Pun;
    using Ilumisoft.SkillDrive.Network;
    using UnityEngine;

    namespace Ilumisoft.SkillDrive
    {
    public class Vehicle : MonoBehaviour
    {
        // 스피드업 효과 관련 필드
        private float speedUpTimer = 0f;
        private float speedUpMultiplier = 1f;
        private float originalAcceleration;
        private float originalMaxSpeed;

        // 스피드업 활성화 여부 (제트엔진 파티클용)
        public bool IsSpeedUpActive => speedUpTimer > 0f;
        public float SpeedUpMultiplier => speedUpMultiplier;
    [SerializeField]
    private VehicleStats stats = new VehicleStats();

    [SerializeField]
    private VehiclePhysics physics = new VehiclePhysics();

    [SerializeField]
    private VehicleGroundDetection groundDetection = new VehicleGroundDetection();

    private VehicleInputManager inputManager = new VehicleInputManager();

    public VehicleStats FinalStats => stats;
    public Rigidbody Rigidbody { get; private set; }
    public Vector2 Input => inputManager.Input;
    public bool IsGrounded => groundDetection.IsGrounded;
    public bool CanMove { get; set; } = true;
    public float ForwardSpeed => Vector3.Dot(Rigidbody.linearVelocity, transform.forward);
    public float NormalizedForwardSpeed => (Mathf.Abs(ForwardSpeed) > 0.1f) ? ForwardSpeed / FinalStats.MaxSpeed : 0.0f;

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        inputManager.Initialize(this);
        groundDetection.Initialize(this);

        var pv = GetComponent<PhotonView>();
        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = pv != null && pv.IsMine;
        }

        // Ensure VehicleSeatManager exists on the same GameObject as the PhotonView on ALL clients
        if (pv != null && PhotonNetwork.IsConnected)
        {
            var seatMgr = pv.gameObject.GetComponent<VehicleSeatManager>();
            if (seatMgr == null)
            {
                pv.gameObject.AddComponent<VehicleSeatManager>();
                Ilumisoft.SkillDrive.SkillDriveDebug.LogVehicle("[Vehicle] Ensured VehicleSeatManager on PhotonView GameObject at Awake", this);
            }

            var camMgr = pv.gameObject.GetComponent<Ilumisoft.SkillDrive.Network.VehicleCameraManager>();
            if (camMgr == null)
            {
                pv.gameObject.AddComponent<Ilumisoft.SkillDrive.Network.VehicleCameraManager>();
                Ilumisoft.SkillDrive.SkillDriveDebug.LogVehicle("[Vehicle] Ensured VehicleCameraManager on PhotonView GameObject at Awake", this);
            }

            if (!PhotonNetwork.IsMasterClient && Rigidbody != null)
            {
                Rigidbody.useGravity = false;
                // 클라이언트: Extrapolate로 미래 위치 예측 → 더 부드러운 움직임
                Rigidbody.interpolation = RigidbodyInterpolation.Extrapolate;
                // 더 빠른 물리 업데이트
                Rigidbody.maxAngularVelocity = 50f; // 기본 7 → 50
            }
            else if (PhotonNetwork.IsMasterClient && Rigidbody != null)
            {
                // 마스터: Interpolate로 안정적의 계산
                Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                Rigidbody.maxAngularVelocity = 50f;
            }
        }
    }

    /// <summary>
    /// Apply lateral friction, steering and acceleration
    /// </summary>
    protected virtual void FixedUpdate()
    {
        // 스피드업 효과 타이머 처리
        if (speedUpTimer > 0f)
        {
            speedUpTimer -= Time.fixedDeltaTime;
            if (speedUpTimer <= 0f)
            {
                // 원래 값 복구
                FinalStats.Acceleration = originalAcceleration;
                FinalStats.MaxSpeed = originalMaxSpeed;
                speedUpMultiplier = 1f;
            }
        }

        CollectInput();
        PerformGroundCheck();
        ApplyCenterOfMass();
        ApplyGravity();
        ApplyLateralFriction();
        ApplySteering();
        ApplyAcceleration();
    }
    /// <summary>
    /// 일정 시간 동안 차량의 가속도/최대속도 배율을 올려주는 스피드업 효과
    /// </summary>
    public void ApplySpeedUp(float duration, float multiplier)
    {
        // 첫 번째 스피드업이면 원래 값 저장
        if (speedUpTimer <= 0f)
        {
            originalAcceleration = FinalStats.Acceleration;
            originalMaxSpeed = FinalStats.MaxSpeed;
        }
        
        // 타이머 추가 (누적)
        speedUpTimer += duration;
        
        // 배율 적용 (매번 새로 적용)
        speedUpMultiplier = multiplier;
        FinalStats.Acceleration = originalAcceleration * multiplier;
        FinalStats.MaxSpeed = originalMaxSpeed * multiplier;
    }

    /// <summary>
    /// 스피드업 효과를 즉시 초기화 (리스폰 시 사용)
    /// </summary>
    public void ResetSpeedUp()
    {
        if (speedUpTimer > 0f)
        {
            // 원래 값으로 복구
            FinalStats.Acceleration = originalAcceleration;
            FinalStats.MaxSpeed = originalMaxSpeed;
            speedUpMultiplier = 1f;
            speedUpTimer = 0f;
        }
    }

    /// <summary>
    /// Collect the input from all input sources
    /// </summary>
    protected virtual void CollectInput()
    {
        inputManager.CollectInput();
    }

    /// <summary>
    /// Checks whether the vehicle is grounded or not
    /// </summary>
    protected virtual void PerformGroundCheck()
    {
        groundDetection.CheckGround();
    }

    /// <summary>
    /// Applies the custom center of mass to the rigidbody
    /// </summary>
    protected virtual void ApplyCenterOfMass()
    {
        if (physics.CenterOfMass != null)
        {
            Rigidbody.centerOfMass = Rigidbody.transform.InverseTransformPoint(physics.CenterOfMass.position);
        }
    }

    /// <summary>
    /// Applies gravity to the vehicle
    /// </summary>
    protected virtual void ApplyGravity()
    {
        if (!CanMove) return;
        float factor;
        if (groundDetection.IsGrounded)
        {
            factor = physics.Gravity;
        }
        else if (groundDetection.IsNearGround)
        {
            factor = Mathf.Lerp(physics.FallGravity, physics.Gravity, 0.7f);
        }
        else
        {
            factor = physics.FallGravity;
        }
        Rigidbody.AddForce(-factor * Vector3.up, ForceMode.Acceleration);
    }

    /// <summary>
    /// Applies side friction to the vehicle.
    /// </summary>
    protected virtual void ApplyLateralFriction()
    {
        if (CanMove && IsGrounded)
        {
            float lateralSpeed = Vector3.Dot(Rigidbody.linearVelocity, transform.right);
            Vector3 lateralFriction = -transform.right * (lateralSpeed / Time.fixedDeltaTime) * FinalStats.Grip;
            Rigidbody.AddForce(lateralFriction, ForceMode.Acceleration);
        }
    }
      
    /// <summary>
    /// Applies the steering to the vehicle by adding rotation force
    /// </summary>
    protected virtual void ApplySteering()
    {
        if (IsGrounded && CanMove)
        {
            float steeringPower = Input.x * FinalStats.SteeringPower;
            float speedFactor = ForwardSpeed * 0.075f;
            steeringPower = Mathf.Clamp(steeringPower * speedFactor, -FinalStats.SteeringPower, FinalStats.SteeringPower);
            float rotationTorque = steeringPower - Rigidbody.angularVelocity.y;
            Rigidbody.AddRelativeTorque(0f, rotationTorque, 0f, ForceMode.VelocityChange);
        }
    }

    /// <summary>
    /// Applies the movement acceleration to the vehicle
    /// </summary>
    protected virtual void ApplyAcceleration()
    {
        if (IsGrounded && CanMove)
        {
            // Arcade Racing Kit style acceleration with drag system
            float drag = FinalStats.Acceleration / FinalStats.MaxSpeed;
            var force = FinalStats.Acceleration * Input.y - drag * Mathf.Clamp(ForwardSpeed, 0f, FinalStats.MaxSpeed);
            Rigidbody.AddForce(transform.forward * force, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Add all required components automatically, when creating a new vehicle
    /// </summary>
    public void Reset()
    {
        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.None;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.mass = 2500;
            rb.angularDamping = 3;
            rb.linearDamping = 0.05f;
        }
        if(GetComponent<VehicleInputSource>() == null)
        {
            gameObject.AddComponent<VehiclePlayerInput>();
        }
    }

    /// <summary>
    /// Draw additional debug info in the scene view when the vehicle is selected
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        groundDetection.OnDrawGizmosSelected(this);
    }
}
}
