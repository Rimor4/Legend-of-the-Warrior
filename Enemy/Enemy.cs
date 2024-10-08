using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(PhysicsCheck))]
public class Enemy : MonoBehaviour
{
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public PhysicsCheck physicsCheck;
    [HideInInspector] public Animator anim;

    [Header("Basic parameter")]
    public float normalSpeed;
    public float chaseSpeed;
    [HideInInspector] public float currentSpeed;
    public Vector3 faceDir;
    public float hurtForce;
    public Transform attacker;
    public Vector3 spawnPoint;

    [Header("Detection")]
    public Vector2 centerOffset;
    public Vector2 checkSize;
    public float checkDistance;
    public LayerMask attackLayer;

    [Header("Timer")]
    public float waitTime;
    public float waitCounter;
    public bool wait;
    public float lostTime;
    public float lostTimeCounter;

    [Header("State")]
    public bool isHurt;
    public bool isDead;

    private BaseState currentState;
    protected BaseState patrolState;
    protected BaseState chaseState;
    protected BaseState skillState;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        physicsCheck = GetComponent<PhysicsCheck>();
        anim = GetComponent<Animator>();

        currentSpeed = normalSpeed;
        waitCounter = waitTime;
        spawnPoint = transform.position;

        lostTimeCounter = lostTime;
    }

    private void OnEnable()
    {
        currentState = patrolState;
        currentState.OnEnter(this);
    }

    private void Update()
    {
        // 图片是朝左的
        faceDir = new Vector3(-transform.localScale.x, 0, 0);

        currentState.LogicUpdate();
        TimeCounter();
    }

    private void FixedUpdate()
    {
        if (!isHurt && !isDead && !wait)
            Move();

        currentState.PhysicsUpdate();
    }

    private void OnDisable()
    {
        currentState.OnExit();
    }

    public virtual void Move()
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("snailPreMove") && !anim.GetCurrentAnimatorStateInfo(0).IsName("snailHideRecover"))
        {
            rb.velocity = new Vector2(currentSpeed * faceDir.x * Time.deltaTime, rb.velocity.y);
        }
    }

    /// <summary>
    /// 计时器
    /// </summary>
    public void TimeCounter()
    {
        if (wait)
        {
            waitCounter -= Time.deltaTime;
            if (waitCounter < 0)
            {
                wait = false;
                waitCounter = waitTime;
                transform.localScale = new Vector3(faceDir.x, 1, 1);
            }
        }

        // 是否丢失追赶对象
        if (lostTimeCounter > 0 && !FoundPlayer())
        {
            lostTimeCounter -= Time.deltaTime;
        }
        else if (FoundPlayer())
        {
            lostTimeCounter = lostTime;
        }
    }

    public virtual bool FoundPlayer()
    {
        // 可作为是否检测成功的bool值
        return Physics2D.BoxCast(transform.position + (Vector3)centerOffset, checkSize, 0, faceDir, checkDistance, attackLayer);
    }

    public void SwitchState(NPCState state)
    {
        BaseState newState = state switch
        {
            NPCState.Patrol => patrolState,
            NPCState.Chase => chaseState,
            NPCState.Skill => skillState,
            _ => null
        };

        currentState.OnExit();
        currentState = newState;
        currentState.OnEnter(this);
    }

    public virtual Vector3 GetNewPoint()
    {
        return transform.position;
    }

    #region 事件执行方法
    public void OnTakeDamage(Transform attackTrans)
    {
        attacker = attackTrans;

        // 转身
        if (attackTrans.position.x - transform.position.x > 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        if (attackTrans.position.x - transform.position.x < 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }

        // 受伤后后退
        isHurt = true;
        anim.SetTrigger("hurt");
        Vector2 dir = new Vector2(transform.position.x - attackTrans.position.x, 0).normalized;

        rb.velocity = new Vector2(0, rb.velocity.y);
        StartCoroutine(OnHurt(dir));
    }

    /// <summary>
    /// 敌人受伤时触发的被击退协程
    /// 
    /// Note: 协程就是一种特殊的函数，它可以主动的请求暂停自身并提交一个唤醒条件，
    /// Unity会在唤醒条件满足的时候去重新唤醒协程。
    /// </summary>
    private IEnumerator OnHurt(Vector2 dir)
    {
        rb.AddForce(dir * hurtForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.5f);
        isHurt = false;
    }

    public void OnDie()
    {
        gameObject.layer = 2;   // Ignore Raycast
        anim.SetBool("dead", true);
        isDead = true;
    }

    // 添加到死亡动画最后一帧
    public void DestroyAfterAnimation()
    {
        Destroy(this.gameObject);
    }
    #endregion

    public virtual void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position + (Vector3)centerOffset + new Vector3(checkDistance * faceDir.x, 0), 0.2f);
    }
}