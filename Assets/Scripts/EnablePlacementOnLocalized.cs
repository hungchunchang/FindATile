using System.Collections;
using System.Collections.Generic;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.PersistentAnchors;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class NewBehaviourScript : MonoBehaviour
{
    private ARLocationManager _arLocationManager;
    private ARPlaneManager _arPlaneManager;
    private ARPlacements _arPlacements;
    // Start is called before the first frame update
    void Start()
    {
        _arLocationManager = FindObjectOfType<ARLocationManager>();
        _arPlaneManager = FindObjectOfType<ARPlaneManager>();
        _arPlacements = FindObjectOfType<ARPlacements>();

        _arPlaneManager.enabled = false;
        _arPlacements.enabled = false;

        _arLocationManager.locationTrackingStateChanged += OnLocalized;
        
    }

    void OnLocalized(ARLocationTrackedEventArgs eventArgs)
    {
        if(eventArgs.Tracking)
        {
            _arPlaneManager.enabled = true;
            _arPlacements.enabled = true;

        }else{
            _arPlaneManager.enabled = false;
            _arPlacements.enabled = false;
        }

        
    }
}
