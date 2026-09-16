using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class Deer : MonoBehaviour
{
    public float roamDistance = 10f;     // ˆÚ“®æ‚Æ‚Ì‹——£
    public float stoppingDistance = 0.3f;
    private Vector3 pointA; // ‰ŠúˆÊ’u
    private Vector3 pointB; // —£‚ê‚½ˆÚ“®æ
    private bool goingToB = true;

    private NavMeshAgent agent;
    private Animator animator;
 

    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animator.SetBool("isWalking", true);
    }
    void Start()
    {
        pointA = transform.position;
        pointB = GetRandomPointNearby(pointA, roamDistance);

        agent.SetDestination(pointB);
    }
    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
        {
            if (goingToB)
                agent.SetDestination(pointA);
            else
                agent.SetDestination(pointB);

            goingToB = !goingToB;
        }
        animator.SetBool("isWalking", agent.velocity.magnitude > 0.1f);
    }

    Vector3 GetRandomPointNearby(Vector3 origin, float distance)
    {
        Vector3 randomDir = Random.insideUnitSphere * distance;
        randomDir.y = 0f;
        Vector3 rawPos = origin + randomDir;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(rawPos, out hit, distance, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return origin; // fallback
    }

}
