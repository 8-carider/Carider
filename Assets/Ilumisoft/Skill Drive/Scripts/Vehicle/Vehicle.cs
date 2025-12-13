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

        // 최대 속도 강제 고정 관련 필드
        private float forceMaxSpeedTimer = 0f;
        private ParticleSystem vehicleSpeedParticles = null;

        [SerializeField, Tooltip("차량 주변에 생성될 스피드업 파티클 프리팹")]
        private GameObject vehicleSpeedParticlePrefab = null;

        // 스피드업 활성화 여부 (제트엔진 파티클용)
        public bool IsSpeedUpActive => speedUpTimer > 0f || forceMaxSpeedTimer > 0f;
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
            }
            else if (PhotonNetwork.IsMasterClient && Rigidbody != null)
            {
                // 마스터: Interpolate로 안정적의 계산
                Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
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
            
            // 매 프레임마다 FinalStats 값 확인 (문제 진단용)
            if (Time.frameCount % 50 == 0) // 50프레임마다 로그 출력
            {
                Debug.Log($"[Vehicle FixedUpdate] 현재 Accel={FinalStats.Acceleration}, MaxSpeed={FinalStats.MaxSpeed}, 타이머={speedUpTimer}");
            }
            
            if (speedUpTimer <= 0f)
            {
                // 원래 값 복구
                FinalStats.Acceleration = originalAcceleration;
                FinalStats.MaxSpeed = originalMaxSpeed;
                speedUpMultiplier = 1f;
                Debug.Log($"[Vehicle] 스피드업 종료, 원래 값 복구: Accel={FinalStats.Acceleration}, MaxSpeed={FinalStats.MaxSpeed}");
            }
        }

        // 최대 속도 강제 고정 타이머 처리
        if (forceMaxSpeedTimer > 0f)
        {
            forceMaxSpeedTimer -= Time.fixedDeltaTime;
            
            // 타이머 종료 시 차량 파티클 제거
            if (forceMaxSpeedTimer <= 0f)
            {
                RemoveVehicleSpeedParticles();
            }
            
            // 현재 속도를 최대 속도로 강제 설정
            if (IsGrounded && CanMove)
            {
                Vector3 velocity = Rigidbody.linearVelocity;
                Vector3 forwardDirection = transform.forward;
                
                // 입력에 따라 방향 결정 (뒤키를 누르면 후진, 아니면 전진)
                float direction = (Input.y < 0) ? -1f : 1f;
                
                // 목표 속도
                float targetSpeed = FinalStats.MaxSpeed * direction;
                
                // 즉시 최대 속도로 설정 (좌우 속도는 유지)
                velocity = forwardDirection * targetSpeed + transform.right * Vector3.Dot(velocity, transform.right);
                Rigidbody.linearVelocity = velocity;
            }
        }

        CollectInput();
        PerformGroundCheck();
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
        Debug.Log($"[Vehicle] ApplySpeedUp 호출됨: duration={duration}, multiplier={multiplier}, 현재 타이머={speedUpTimer}");
        
        // 첫 번째 스피드업이면 원래 값 저장
        if (speedUpTimer <= 0f)
        {
            originalAcceleration = FinalStats.Acceleration;
            originalMaxSpeed = FinalStats.MaxSpeed;
            Debug.Log($"[Vehicle] 원래 값 저장: Accel={originalAcceleration}, MaxSpeed={originalMaxSpeed}");
        }
        
        // 타이머 추가 (누적)
        speedUpTimer += duration;
        
        // 배율 적용 (매번 새로 적용)
        speedUpMultiplier = multiplier;
        FinalStats.Acceleration = originalAcceleration * multiplier;
        FinalStats.MaxSpeed = originalMaxSpeed * multiplier;
        
        Debug.Log($"[Vehicle] 스피드업 적용 완료: 새 Accel={FinalStats.Acceleration}, 새 MaxSpeed={FinalStats.MaxSpeed}, 타이머={speedUpTimer}");
    }

    /// <summary>
    /// 일정 시간 동안 차량을 원래 최대 속도로 강제 고정
    /// </summary>
    public void ForceMaxSpeed(float duration)
    {
        // Photon RPC로 모든 클라이언트에 동기화
#if PHOTON_UNITY_NETWORKING
        PhotonView photonView = PhotonView.Get(this);
        if (photonView != null && PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_ForceMaxSpeed", RpcTarget.All, duration);
        }
        else
        {
            // 싱글플레이 또는 PhotonView 없을 때
            ApplyForceMaxSpeed(duration);
        }
#else
        ApplyForceMaxSpeed(duration);
#endif
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_ForceMaxSpeed(float duration)
    {
        ApplyForceMaxSpeed(duration);
    }
#endif

    /// <summary>
    /// 최대 속도 강제 고정 효과 실제 적용
    /// </summary>
    private void ApplyForceMaxSpeed(float duration)
    {
        forceMaxSpeedTimer = duration;
        CreateVehicleSpeedParticles();
    }

    /// <summary>
    /// 차량 주변에 스피드 파티클 생성
    /// </summary>
    private void CreateVehicleSpeedParticles()
    {
        if (vehicleSpeedParticlePrefab == null) return;
        
        // 이미 존재하면 재사용
        if (vehicleSpeedParticles != null)
        {
            vehicleSpeedParticles.Play();
            return;
        }

        // 차량의 자식으로 파티클 생성
        GameObject particleObj = Instantiate(vehicleSpeedParticlePrefab, transform);
        particleObj.transform.localPosition = Vector3.zero;
        particleObj.transform.localRotation = Quaternion.identity;
        
        vehicleSpeedParticles = particleObj.GetComponent<ParticleSystem>();
        if (vehicleSpeedParticles != null)
        {
            vehicleSpeedParticles.Play();
        }
    }

    /// <summary>
    /// 차량 스피드 파티클 제거
    /// </summary>
    private void RemoveVehicleSpeedParticles()
    {
        if (vehicleSpeedParticles != null)
        {
            vehicleSpeedParticles.Stop();
            Destroy(vehicleSpeedParticles.gameObject, 2f); // 2초 후 삭제 (파티클이 자연스럽게 사라지도록)
            vehicleSpeedParticles = null;
        }
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
        
        // 최대 속도 강제 효과도 초기화
        forceMaxSpeedTimer = 0f;
        RemoveVehicleSpeedParticles();
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
            var force = FinalStats.Acceleration * Input.y;
            if (Mathf.Abs(force) > 0.01f)
            {
                Rigidbody.AddForce(transform.forward * force, ForceMode.Acceleration);
            }
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
