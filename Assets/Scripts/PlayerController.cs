using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    public float velocity, runVelocity, walkVelocity, velocityCoEfficient;
    public float angularVelocity;
    private float InputVertical, InputHorizontal;
    private float MouseAxisHorizontal, MouseAxisVertical;
    private float MouseSensitivity;
    public float jumpForece, jumpCoEfficient;
    private bool llJump;
    private bool isGrounded;
    private int selectedGunIndex;
    public float minViewAngle;
    public float maxViewAngle;
    public float gravity;
    public float maxHeat;
    public float minHeat;
    public float currentHeat;
    public float coolAmount, coolCoEfficient;
    public bool isOverHeated;
    public float WeponcoolDownTime;
    public float healt;
    public float MaxHealt; // may update later 
    private List<Gun> gunList;
    private Gun selectedGun;

    // test
    [SerializeField] public float handRotationAngle = -60f;

    public GameObject head; // assing from editor
    public GameObject raycaster; // assing from editor
    public LayerMask groundLayer; // assing from editor
    public List<GameObject> gunObjects; // assign from editor
    public GameObject gunPlaceHodler; // assing from editor
    public Animator animator; // assing from editor
    public GameObject bulletImpactPrefab;// assing from editor
    public GameObject bloodImpactPrefab; // assing form editor

    // network related
    PhotonView photonView;
    void Start()
    {
        this.runVelocity = 8.5f * 30;
        this.walkVelocity = 6f * 30;
        velocityCoEfficient = 1f;
        this.velocity = this.walkVelocity;
        this.angularVelocity = 30f;
        this.jumpForece = 500f;
        this.jumpCoEfficient = 1;
        this.gravity = 10f;
        this.rb = this.GetComponent<Rigidbody>();
        this.llJump = false;
        this.isGrounded = true;
        this.minViewAngle = -60;
        this.maxViewAngle = 60;
        this.selectedGunIndex = 0;
        this.maxHeat = 100f;
        this.minHeat = 0f;
        this.coolAmount = 30f;
        this.coolCoEfficient = 1f;
        this.currentHeat = 0f; //inital heat amount
        this.isOverHeated = false;
        this.gunList = Gun.CreateGuns().ToList<Gun>();
        this.MaxHealt = 100f;
        this.healt = this.MaxHealt;
        MouseSensitivity = 1f; // player prefab  default is 1
        MouseSensitivity = 10f;
        photonView = this.GetComponent<PhotonView>();

        SetSelectedGunIndex(0);

        GameUIController.instance.SetHeatSliderValues(this.minHeat, this.maxHeat);
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            //read keys
            GetAxis();
            ReadJumpKey();
            //

            AdjustVelocity();
            TurnPlayerBody();
            RotateHead();

            SwitchGuns();
            Shoot();
            CoolDown();
            UpdateWeponCoolDownTime();
        }

    }
    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            MovePlayer();
            ApplyGravity();
            Jump();
            checkIsGournded();
        }

    }
    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            SyncCam();
            SyncGunPositions();
            SyncAnimation();
            GameUIController.instance.UpdateHeatSliderValue(this.currentHeat);
            GameUIController.instance.UpdateHealtAmountLable(this.healt);
        }
    }
    private void MovePlayer()
    {
        Vector3 dVertical = transform.forward * this.InputVertical;
        Vector3 dHorizontal = transform.right * this.InputHorizontal;
        Vector3 dV = dVertical + dHorizontal;
        dV = dV.normalized;

        dV = dV * Time.deltaTime * this.velocity * velocityCoEfficient;

        this.rb.velocity = new Vector3(dV.x, this.rb.velocity.y, dV.z);
    }
    private void TurnPlayerBody()
    {
        Vector3 rotationVector = new Vector3(0, 1, 0) * Time.deltaTime * MouseAxisHorizontal * this.angularVelocity * MouseSensitivity;
        this.transform.Rotate(rotationVector, Space.Self);
    }
    private void GetAxis()
    {
        InputVertical = Input.GetAxisRaw("Vertical");
        InputHorizontal = Input.GetAxisRaw("Horizontal");
        MouseAxisHorizontal = Input.GetAxisRaw("Mouse X");
        MouseAxisVertical = Input.GetAxisRaw("Mouse Y");
    }
    private void SyncCam()
    {
        Camera.main.transform.position = head.transform.position;
        Camera.main.transform.rotation = head.transform.rotation;
    }
    private void AdjustVelocity()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift)) this.velocity = this.runVelocity;
        if (Input.GetKeyUp(KeyCode.LeftShift)) this.velocity = this.walkVelocity;
    }

    private void Jump()
    {
        if (llJump && isGrounded)
        {
            this.rb.AddForce(Vector3.up * jumpForece);
            llJump = false;
            isGrounded = false;
        }
    }
    private void ApplyGravity()
    {
        float deltavy = this.gravity * Time.deltaTime * -1;
        Vector3 velocity = this.rb.velocity;
        velocity.y += deltavy;
        this.rb.velocity = velocity;
    }
    private void ReadJumpKey()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) llJump = true;
    }
    private void checkIsGournded()
    {
        this.isGrounded = Physics.Raycast(this.raycaster.transform.position, Vector3.down, 0.20f, groundLayer);
    }
    private void RotateHead()
    {
        Vector3 rotationVector = this.head.transform.rotation.eulerAngles;
        rotationVector += new Vector3(1f, 0, 0) * this.MouseAxisVertical * 3f * -1;
        float x = rotationVector.x;
        if (x > 180)
            x -= 360;
        x = Mathf.Clamp(x, minViewAngle, maxViewAngle);
        if (x < 0)
            x += 360;
        rotationVector.x = x;

        this.head.transform.rotation = Quaternion.Euler(rotationVector);
    }

    private void SyncGunPositions()
    {
        gunPlaceHodler.transform.rotation = head.transform.rotation;
        gunPlaceHodler.transform.Rotate(new Vector3(0, 180f, 0), Space.Self);

        // sync gun posiiton on other players in network
        photonView.RPC(nameof(SyncGunPositionOnNetwork), RpcTarget.All, gunPlaceHodler.transform.position, gunPlaceHodler.transform.rotation);
    }

    private void DeactiveteAllGuns()
    {
        foreach (var gunObj in gunObjects)
        {
            gunObj.SetActive(false);
        }
    }
    private void SelectGun()
    {
        gunObjects[selectedGunIndex].SetActive(true);
        selectedGun = gunList[selectedGunIndex];
    }
    private void SetSelectedGunIndex(int i)
    {
        DeactiveteAllGuns();
        this.selectedGunIndex = Mathf.Clamp(i, 0, 2);
        SelectGun();
    }
    private void SwitchGuns()
    {
        //  update selected gun and selected gun index
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetSelectedGunIndex(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SetSelectedGunIndex(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SetSelectedGunIndex(2);
        else if (Input.GetAxis("Mouse ScrollWheel") > 0f) // forward
            SetSelectedGunIndex(selectedGunIndex + 1);
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f) // backwards
            SetSelectedGunIndex(selectedGunIndex - 1);
        // after update
        // switch gun on all instace of users in network
        photonView.RPC(nameof(SelectGunOnNewtork), RpcTarget.All, selectedGunIndex);
    }

    private void Shoot()
    {
        bool flag = (!selectedGun.isAutomatic && Input.GetMouseButtonDown(0) || selectedGun.isAutomatic && Input.GetMouseButton(0)) && !isOverHeated && WeponcoolDownTime <= 0;
        if (flag)
        {
            Ray rotatedRay = Camera.main.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            rotatedRay.origin = Camera.main.transform.position;
            RaycastHit hit;
            if (Physics.Raycast(rotatedRay, out hit, Mathf.Infinity))
            {
                Debug.Log(hit.transform.gameObject.tag);
                if (hit.transform.gameObject.tag == "obstacle")
                {
                    GameObject bulletImpact = Instantiate(bulletImpactPrefab, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up)) as GameObject;
                    bulletImpact.transform.Rotate(new Vector3(90f, 0, 0), Space.Self);
                    Destroy(bulletImpact, 10f);
                    photonView.RPC(nameof(SyncGunShoootImpact), RpcTarget.All, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                }
                if (hit.transform.gameObject.tag == "Player")
                {
                    hit.transform.gameObject.GetPhotonView().RPC(nameof(TakeDamge) , RpcTarget.All , this.selectedGun.damagePerShot);
                    //photonView.RPC(nameof(SyncBloodEffect) , RpcTarget.All , hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up))
                    GameObject particleEffect = PhotonView.Instantiate(bloodImpactPrefab, hit.point + (hit.normal * .002f) , Quaternion.LookRotation(hit.normal, Vector3.up));
                    Destroy(particleEffect, 3f);
                }
            }
            this.WeponcoolDownTime = this.selectedGun.coolDownTime;
            this.currentHeat += this.selectedGun.heatPerShot;
            currentHeat = Mathf.Clamp(currentHeat, 0, 100);
            if (currentHeat == 100 && !isOverHeated)
            {
                isOverHeated = true;
                GameUIController.instance.ShowOverHeatLable();
            }
        }
    }

    private void SyncAnimation()
    {
        animator.SetBool("grounded", isGrounded);
        animator.SetFloat("speed", this.rb.velocity.magnitude);
    }

    private void CoolDown()
    {
        this.currentHeat -= Time.deltaTime * coolAmount * coolCoEfficient * (isOverHeated ? 2 : 1);
        this.currentHeat = Mathf.Clamp(currentHeat, 0, 100f);
        if (isOverHeated && currentHeat == 0) isOverHeated = false;
    }

    private void UpdateWeponCoolDownTime()
    {
        this.WeponcoolDownTime -= Time.deltaTime;
        if (WeponcoolDownTime < 0) WeponcoolDownTime = 0;
    }

    #region network related

    [PunRPC]
    public void SelectGunOnNewtork(int selectedIndex)
    {
        SetSelectedGunIndex(selectedIndex);
    }
    [PunRPC]
    public void SyncGunPositionOnNetwork(Vector3 position, Quaternion rotation)
    {
        gunPlaceHodler.transform.position = position;
        gunPlaceHodler.transform.rotation = rotation;
    }
    [PunRPC]
    public void SyncGunShoootImpact(Vector3 position, Quaternion rotation)
    {
        GameObject bulletImpact = Instantiate(bulletImpactPrefab, position, rotation) as GameObject;
        bulletImpact.transform.Rotate(new Vector3(90f, 0, 0), Space.Self);
        Destroy(bulletImpact, 10f);
    }

    [PunRPC]    
    public void TakeDamge(float damageAmount)
    {
        this.healt -= damageAmount;
    }
    #endregion
}
