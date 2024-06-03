using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterestZone : MonoBehaviour
{
    public string interestAction;
    public GameObject actionIndicatorPrefab;

    private InterestLocation[] locations;

    private void Start()
    {
        locations = GetComponentsInChildren<InterestLocation>();
    }

    private void OnTriggerEnter(Collider other)
    {
        PedestrianBehaviour roamMovement = other.GetComponent<PedestrianBehaviour>();
        if (roamMovement != null)
        {
            roamMovement.currentZone = this;
            //roamMovement.EnterInterestZone(this);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        PedestrianBehaviour roamMovement = other.GetComponent<PedestrianBehaviour>();
        if (roamMovement != null)
        {
            roamMovement.currentZone = null;
            //roamMovement.EnterInterestZone(this);
        }
    }
    public Transform GetAvailableLocation()
    {
        foreach (var location in locations)
        {
            if (location.Occupy())
            {
                return location.transform;
            }
        }
        return null;
    }

    public void VacateLocation(Transform locationTransform)
    {
        foreach (var location in locations)
        {
            if (location.transform == locationTransform)
            {
                location.Vacate();
                break;
            }
        }
    }
}
