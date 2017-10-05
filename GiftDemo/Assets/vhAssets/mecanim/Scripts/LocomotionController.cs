using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class LocomotionController : MonoBehaviour
{
    #region Constants
    public delegate void OnReachedDestination(LocomotionController locomoter);
    public delegate void OnPathingUpdate(LocomotionController controller);
    #endregion

    #region Variables
    //[SerializeField]
    //GameObject goal;
    [SerializeField] NavMeshAgent m_Agent;
    [SerializeField] Animator m_AnimatingAgent;
    [SerializeField] string m_IsLomotingParamName = "Locomoting";
    [SerializeField] string m_LocomotionSpeedParamName = "Speed";
    [SerializeField] string m_LocomotionDirectionParamName = "Direction";
    [SerializeField] float m_AnimationSpeedNormalizer = 1;
    [SerializeField] float m_AnimationDirectionNormalizer = 1;
    NavMeshPath m_Path;
    OnReachedDestination m_OnReachedDestinationCB;
    OnPathingUpdate m_OnPathingUpdate;
    #endregion

    #region Properties
    public NavMeshAgent Agent { get { return m_Agent; } }
    public float Speed
    {
        get { return Agent.speed; }
        set { Agent.speed = value; }
    }
    public float AngularSpeed
    {
        get { return Agent.angularSpeed; }
        set { Agent.angularSpeed = value; }
    }

    #endregion

    #region Functions
    void Awake()
    {
        m_Path = new NavMeshPath();
    }

    void Start()
    {
        if (m_Agent == null)
        {
            m_Agent = GetComponent<NavMeshAgent>();
        }
    }

    public void AddOnReachedDestinationCallback(OnReachedDestination cb)
    {
        m_OnReachedDestinationCB += cb;
    }

    public void AddonPathingUpdateCallback(OnPathingUpdate cb)
    {
        m_OnPathingUpdate += cb;
    }

    void Update()
    {
        
        if (!m_Agent.pathPending)
        {
            if (!m_Agent.isStopped && m_Agent.remainingDistance <= m_Agent.stoppingDistance)
            {
                if ((!m_Agent.hasPath || m_Agent.velocity.sqrMagnitude == 0f) && m_AnimatingAgent.GetBool(m_IsLomotingParamName))
                {
                    // Done
                    ReachedDestination();
                }
            }
        }

        // Check if we've reached the destination
        if (m_Agent.hasPath && m_AnimatingAgent.GetBool(m_IsLomotingParamName))
        { 
            m_AnimatingAgent.SetFloat(m_LocomotionSpeedParamName, m_Agent.velocity.magnitude / m_AnimationSpeedNormalizer);
            
            DetermineAnimationDirection();

            if (m_OnPathingUpdate != null)
            {
                m_OnPathingUpdate(this);
            }
        }

        

        /*if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            WalkTo(goal);
        }*/
    }

    public void Warp(Vector3 destination)
    {
        if (!m_Agent.Warp(destination))
        {
            Debug.LogErrorFormat("Failed to warp {0} to destination {1}", m_Agent.name, destination);
        }
    }

    public void WalkTo(Transform destination)
    {
        WalkTo(destination.position);
    }

    public void WalkTo(GameObject destination)
    {
        WalkTo(destination.transform.position);
    }

    public void WalkTo(Vector3 destination)
    {
        m_Agent.SetDestination(destination);
        m_AnimatingAgent.SetBool(m_IsLomotingParamName, true);
    }

    public void SetPath(Vector3 destination)
    {
        SetPath(m_Agent.transform.position, destination);
    }

    public void SetPath(Vector3 startingPosition, Vector3 destination)
    {
        if (m_Path == null)
        {
            m_Path = new NavMeshPath();
        }

        if (!NavMesh.CalculatePath(startingPosition, destination, NavMesh.AllAreas, m_Path))
        {
            Debug.LogErrorFormat("Agent {0} failed to Calculate path from {1} to {2}", m_Agent.name, startingPosition, destination);
        }
        m_Agent.SetPath(m_Path);
        m_AnimatingAgent.SetBool(m_IsLomotingParamName, true);
    }

    void DetermineAnimationDirection()
    {
        Vector3 movementDir = m_Agent.destination - transform.position;
        float dotProduct = Vector3.Dot(transform.right, movementDir);
        if (dotProduct > 0)
        {
            // turn right
            m_AnimatingAgent.SetFloat(m_LocomotionDirectionParamName, m_Agent.velocity.y / m_AnimationDirectionNormalizer);
        }
        else if (dotProduct < 0)
        {
            // turn left
            m_AnimatingAgent.SetFloat(m_LocomotionDirectionParamName, -m_Agent.velocity.y / m_AnimationDirectionNormalizer);
        }
        else
        {
            // no turning required
        }
    }


    void ReachedDestination()
    {
        m_AnimatingAgent.SetBool(m_IsLomotingParamName, false);
        m_AnimatingAgent.SetFloat(m_LocomotionSpeedParamName, 0);
        m_AnimatingAgent.SetFloat(m_LocomotionDirectionParamName, 0);
        //m_AnimatingAgent.set

        if (m_OnReachedDestinationCB != null)
        {
            m_OnReachedDestinationCB(this);
        }
    }
	
	public static float CalculatePathLength(Vector3 startingPosition, Vector3 destination)
    {
        // Create a path and set it based on a target position.
        NavMeshPath path = new NavMeshPath();

        NavMesh.CalculatePath(startingPosition, destination, NavMesh.AllAreas, path);

        // Create an array of points which is the length of the number of corners in the path + 2.
        Vector3[] allWayPoints = new Vector3[path.corners.Length + 2];

        // The first point is the enemy's position.
        allWayPoints[0] = startingPosition;

        // The last point is the target position.
        allWayPoints[allWayPoints.Length - 1] = destination;

        // The points inbetween are the corners of the path.
        for (int i = 0; i < path.corners.Length; i++)
        {
            allWayPoints[i + 1] = path.corners[i];
        }

        // Create a float to store the path length that is by default 0.
        float pathLength = 0;

        // Increment the path length by an amount equal to the distance between each waypoint and the next.
        for (int i = 0; i < allWayPoints.Length - 1; i++)
        {
            pathLength += Vector3.Distance(allWayPoints[i], allWayPoints[i + 1]);
        }

        return pathLength;
    }
    #endregion
}
