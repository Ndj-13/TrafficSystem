using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using BehaviourAPI.Core;
using BehaviourAPI.StateMachines;
using BehaviourAPI.BehaviourTrees;
using UnityEditor.Search.Providers;

public class PedestrianBehaviour : MonoBehaviour
{
    public NavMeshAgent agent;
    public float range; // Radius of sphere
    public Transform centrePoint; // Centre of the area the agent wants to move around in

    private Animator animator;
    private static readonly int IsWalking = Animator.StringToHash("isWalking");

    private GameObject currentActionIndicator;
    public InterestZone currentZone;
    private Transform currentLocation;

    Transform location;
    private bool isActionCompleted = false;

    private bool inTrafficLight = false;
    private bool isCrossingStreet = false;
    private TrafficLights trafficLight;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator not found!");
        }

        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        if (centrePoint == null)
        {
            centrePoint = transform;
        }
    }
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.TryGetComponent<TrafficLights>(out TrafficLights semaforo))
        {
            trafficLight = semaforo;
            if (!semaforo.IsRedForCars() || semaforo.TimeToChange() < 5)
            {
                inTrafficLight = true;
            }
        }
        inTrafficLight = false;
    }
    private void OnTriggerExit(Collider other)
    {
        trafficLight = null;
    }

    private bool InTrafficLight()
    {
        return inTrafficLight;
    }

    #region FSM States

    // Roaming State
    public void StartRoaming()
    {
        StartCoroutine(Roam());
    }

    private IEnumerator Roam()
    {
        while (true)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                Vector3 point;
                if (RandomPoint(centrePoint.position, range, out point))
                {
                    Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
                    agent.SetDestination(point);
                }
            }

            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool(IsWalking, isMoving);

            yield return null;
        }
    }

    // CrossingStreet State
    public void StartCrossingStreet()
    {
        StartCoroutine(WaitForGreenLight());
    }

    private IEnumerator WaitForGreenLight()
    {
        while (!trafficLight.IsRedForCars())
        {
            yield return new WaitForSeconds(0.5f); // Wait for 0.5 second before checking again
        }

        isCrossingStreet = true;
    }
    #endregion

    public void MoveTo(Vector3 destination)
    {
        agent.SetDestination(destination);
        StartCoroutine(CheckIfArrived());
    }

    private IEnumerator CheckIfArrived()
    {
        while (true)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.isStopped = true;
                yield break; // Terminar coroutine cuando el agente llega al destino
            }

            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool(IsWalking, isMoving);

            yield return null;
        }
    }

    public void PerformAction(string action)
    {
        switch (action)
        {
            case "DrinkCoffee":
                Debug.Log("Tomando un café.");
                break;
            case "RunAround":
                Debug.Log("Dando una vuelta corriendo.");
                break;
            default:
                Debug.LogWarning("Acción desconocida: " + action);
                break;
        }
    }

    public Status EnterInterestZone(InterestZone zone)
    {
        currentZone = zone;
        location = zone.GetAvailableLocation();

        if (location != null )
        {
            return Status.Success;
        }
        return Status.Failure;
        
    }
    public Status MoveToLocation(InterestZone zone)
    {
        currentLocation = location;
        MoveTo(location.position);  
        if(isPathComplete())
        {
            return Status.Success ;
        }
        else
        {
            return Status.Running ;
        }
    }

    public Status PerformActionBT(InterestZone zone)
    {
        isActionCompleted = false;
        StartCoroutine(PerformInterestAction(zone));
        return Status.Success ;  
    }

    public bool IsInterestZone()
    {
        if(currentZone != null)
        {
            return true;
        }
        return false; 
    }

    public bool IsInCoffeeShop()
    {
        if (currentZone.interestAction == "Coffee")
        {
            return true;
        }
        return false;
    }

    public bool IsPark()
    {
        if (currentZone.interestAction == "Park")
        {
            return true;
        }
        return false;
    }

    private IEnumerator PerformInterestAction(InterestZone zone)
    {
        while (agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        agent.isStopped = true;
        PerformAction(zone.interestAction);

        if (currentActionIndicator != null)
        {
            Destroy(currentActionIndicator);
        }

        if (zone.actionIndicatorPrefab != null)
        {
            currentActionIndicator = Instantiate(zone.actionIndicatorPrefab, transform);
            // Ajustar la posición de la imagen por encima del personaje
            currentActionIndicator.transform.localPosition = new Vector3(0, 2.0f, 0);
        }

        // Simular duración de la acción
        yield return new WaitForSeconds(5.0f); // Por ejemplo, 5 segundos para tomar café

        if (currentActionIndicator != null)
        {
            Destroy(currentActionIndicator);
        }

        agent.isStopped = false;
        zone.VacateLocation(currentLocation); // Liberar la ubicación
        currentLocation = null;
        isActionCompleted = true;
    }

    private bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    #region Behaviour Tree Nodes

    public Status CheckIfCanPerformAction()
    {
        // Logic to check if the action can be performed
        if (currentZone != null)
        {
            return Status.Success;
        }
        return Status.Failure;
    }

    public Status PerformActionNode()
    {
        if (currentZone != null)
        {
            PerformAction(currentZone.interestAction);
            return Status.Success;
        }
        return Status.Failure;
    }

    public Status CheckIfCanCrossStreet()
    {
        return trafficLight.IsRedForCars() ? Status.Success : Status.Failure;
    }

    #endregion
    public bool isPathComplete()
    {
        return (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }
}