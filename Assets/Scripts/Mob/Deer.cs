using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class Deer : MonoBehaviour
{
    [Header("移動")]
    public float roamDistance = 30f;
    public float stoppingDistance = 0.3f;

    [Header("スタック対策")]
    public float stuckCheckTime = 2f;
    public float stuckVelocity = 0.05f;

    private NavMeshAgent agent;
    private Animator animator;

    private float stuckTimer = 0f;


    private void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 鹿ごとに優先順位を変える
        // 数字が小さいほど優先される
        agent.avoidancePriority =
            Random.Range(20, 80);

        // 鹿同士をなるべく回避
        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        animator.SetBool(
            "isWalking",
            true
        );
    }


    private void Start()
    {
        TrySetRandomDestination();
    }


    private void Update()
    {
        if (!agent.enabled ||
            !agent.isOnNavMesh)
        {
            return;
        }


        // -------------------------
        // 到着
        // -------------------------

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance <=
                stoppingDistance)
        {
            TrySetRandomDestination();
            return;
        }


        // -------------------------
        // スタック判定
        // -------------------------

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance >
                stoppingDistance)
        {
            if (agent.velocity.magnitude <
                stuckVelocity)
            {
                stuckTimer +=
                    Time.deltaTime;
            }
            else
            {
                stuckTimer = 0f;
            }


            // 一定時間ほぼ動いていなかった
            if (stuckTimer >=
                stuckCheckTime)
            {
                ResolveStuck();
            }
        }
        else
        {
            stuckTimer = 0f;
        }


        // -------------------------
        // アニメーション
        // -------------------------

        animator.SetBool(
            "isWalking",
            agent.velocity.magnitude > 0.1f
        );
    }


    private void ResolveStuck()
    {
        stuckTimer = 0f;

        // 優先順位を変えて
        // 膠着状態を崩す
        agent.avoidancePriority =
            Random.Range(20, 80);

        agent.ResetPath();

        TrySetRandomDestination();
    }


    private bool TrySetRandomDestination()
    {
        const int maxAttempts = 10;


        for (int i = 0;
             i < maxAttempts;
             i++)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle *
                roamDistance;


            Vector3 randomPosition =
                transform.position +
                new Vector3(
                    randomCircle.x,
                    0f,
                    randomCircle.y
                );


            // ランダム地点付近のNavMeshを探す
            if (!NavMesh.SamplePosition(
                    randomPosition,
                    out NavMeshHit hit,
                    2f,
                    NavMesh.AllAreas))
            {
                continue;
            }


            // 「NavMesh上にある」だけでなく、
            // 今いる位置から本当に到達可能か確認
            NavMeshPath path =
                new NavMeshPath();


            if (!agent.CalculatePath(
                    hit.position,
                    path))
            {
                continue;
            }


            if (path.status !=
                NavMeshPathStatus.PathComplete)
            {
                continue;
            }


            agent.SetDestination(
                hit.position
            );

            stuckTimer = 0f;

            return true;
        }


        // 移動先が見つからなかった
        agent.ResetPath();

        return false;
    }
}
