﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using GoogleARCore.CrossPlatform;
using GoogleARCore.Examples.CloudAnchor;

#if UNITY_EDITOR
    using Input = GoogleARCore.InstantPreviewInput;
#endif

public class AnchorController : MonoBehaviour {

    [SerializeField]
    RoomServer RoomServer;
    [SerializeField]
    CloudAnchorUIController UIController;

    [SerializeField]
    GameObject ARCoreRoot;
    [SerializeField]
    GameObject ARKitRoot;
    [SerializeField]
    Camera ARKitFirstPersonCamera;

    const string k_LoopbackIpAddress = "127.0.0.1";

    ARKitHelper m_ARKit = new ARKitHelper();

    bool m_IsQuitting = false;

    Component m_LastPlacedAnchor = null;
    Component m_LastResolvedAnchor = null;

    ApplicationMode m_CurrentMode = ApplicationMode.Ready;
    int m_CurrentRoom;

    public enum ApplicationMode
    {
        Ready,
        Hosting,
        Resolving
    }

    void Start()
    {
        if (Application.platform != RuntimePlatform.IPhonePlayer)
        {
            ARCoreRoot.SetActive(true);
            ARKitRoot.SetActive(false);
        }
        else
        {
            ARCoreRoot.SetActive(false);
            ARKitRoot.SetActive(true);
        }

        _ResetStatus();
    }

    void Update()
    {
        _UpdateApplicationLifecycle();

        // If we are not in hosting mode or the user has already placed an anchor then the update
        // is complete.
        if (m_CurrentMode != ApplicationMode.Hosting || m_LastPlacedAnchor != null)
        {
            return;
        }

        // If the player has not touched the screen then the update is complete.
        Touch touch;
        if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
        {
            return;
        }

        // Raycast against the location the player touched to search for planes.
        if (Application.platform != RuntimePlatform.IPhonePlayer)
        {
            TrackableHit hit;
            if (Frame.Raycast(touch.position.x, touch.position.y,
                    TrackableHitFlags.PlaneWithinPolygon, out hit))
            {
                m_LastPlacedAnchor = hit.Trackable.CreateAnchor(hit.Pose);
            }
        }
        else
        {
            Pose hitPose;
            if (m_ARKit.RaycastPlane(ARKitFirstPersonCamera, touch.position.x, touch.position.y, out hitPose))
            {
                m_LastPlacedAnchor = m_ARKit.CreateAnchor(hitPose);
            }
        }

        if (m_LastPlacedAnchor != null)
        {
            // Save cloud anchor.
            _HostLastPlacedAnchor();

            // do stuff
        }
    }

    /// <summary>
    /// Handles user intent to enter a mode where they can place an anchor to host or to exit this mode if
    /// already in it.
    /// </summary>
    public void OnEnterHostingModeClick()
    {
        if (m_CurrentMode == ApplicationMode.Hosting)
        {
            m_CurrentMode = ApplicationMode.Ready;
            _ResetStatus();
            return;
        }

        m_CurrentMode = ApplicationMode.Hosting;
        m_CurrentRoom = Random.Range(1, 9999);
        UIController.SetRoomTextValue(m_CurrentRoom);
        UIController.ShowHostingModeBegin();
    }

    /// <summary>
    /// Handles a user intent to enter a mode where they can input an anchor to be resolved or exit this mode if
    /// already in it.
    /// </summary>
    public void OnEnterResolvingModeClick()
    {
        if (m_CurrentMode == ApplicationMode.Resolving)
        {
            m_CurrentMode = ApplicationMode.Ready;
            _ResetStatus();
            return;
        }

        m_CurrentMode = ApplicationMode.Resolving;
        UIController.ShowResolvingModeBegin();
    }

    /// <summary>
    /// Handles the user intent to resolve the cloud anchor associated with a room they have typed into the UI.
    /// </summary>
    public void OnResolveRoomClick()
    {
        var roomToResolve = UIController.GetRoomInputValue();
        if (roomToResolve == 0)
        {
            UIController.ShowResolvingModeBegin("Invalid room code.");
            return;
        }

        string ipAddress =
            UIController.GetResolveOnDeviceValue() ? k_LoopbackIpAddress : UIController.GetIpAddressInputValue();

        UIController.ShowResolvingModeAttemptingResolve();
        RoomSharingClient roomSharingClient = new RoomSharingClient();
        roomSharingClient.GetAnchorIdFromRoom(roomToResolve, ipAddress, (bool found, string cloudAnchorId) =>
        {
            if (!found)
            {
                UIController.ShowResolvingModeBegin("Invalid room code.");
            }
            else
            {
                _ResolveAnchorFromId(cloudAnchorId);
            }
        });
    }

    /// <summary>
    /// Hosts the user placed cloud anchor and associates the resulting Id with the current room.
    /// </summary>
    private void _HostLastPlacedAnchor()
    {
#if !UNITY_IOS
        var anchor = (Anchor)m_LastPlacedAnchor;
#else
            var anchor = (UnityEngine.XR.iOS.UnityARUserAnchorComponent)m_LastPlacedAnchor;
#endif
        // store anchor information to singleton
        Singleton.instance.anchor = anchor.transform;
        Debug.Log("anchor: " + Singleton.instance.anchor);

        UIController.ShowHostingModeAttemptingHost();
        XPSession.CreateCloudAnchor(anchor).ThenAction(result =>
        {
            if (result.Response != CloudServiceResponse.Success)
            {
                UIController.ShowHostingModeBegin(
                    string.Format("Failed to host cloud anchor: {0}", result.Response));
                return;
            }

            RoomServer.SaveCloudAnchorToRoom(m_CurrentRoom, result.Anchor);
            UIController.ShowHostingModeBegin("Cloud anchor was created and saved.");
        });
    }

    /// <summary>
    /// Resolves an anchor id and instantiates an Andy prefab on it.
    /// </summary>
    /// <param name="cloudAnchorId">Cloud anchor id to be resolved.</param>
    private void _ResolveAnchorFromId(string cloudAnchorId)
    {
        XPSession.ResolveCloudAnchor(cloudAnchorId).ThenAction((System.Action<CloudAnchorResult>)(result =>
        {
            if (result.Response != CloudServiceResponse.Success)
            {
                UIController.ShowResolvingModeBegin(string.Format("Resolving Error: {0}.", result.Response));
                return;
            }

            m_LastResolvedAnchor = result.Anchor;

            //Instantiate(_GetAndyPrefab(), result.Anchor.transform);
            UIController.ShowResolvingModeSuccess();
        }));
    }

    /// <summary>
    /// Resets the internal status and UI.
    /// </summary>
    private void _ResetStatus()
    {
        // Reset internal status.
        m_CurrentMode = ApplicationMode.Ready;
        if (m_LastPlacedAnchor != null)
        {
            Destroy(m_LastPlacedAnchor.gameObject);
        }

        m_LastPlacedAnchor = null;
        if (m_LastResolvedAnchor != null)
        {
            Destroy(m_LastResolvedAnchor.gameObject);
        }

        m_LastResolvedAnchor = null;
        UIController.ShowReadyMode();
    }

    /// <summary>
    /// Check and update the application lifecycle.
    /// </summary>
    private void _UpdateApplicationLifecycle()
    {
        // Exit the app when the 'back' button is pressed.
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        var sleepTimeout = SleepTimeout.NeverSleep;

#if !UNITY_IOS
        // Only allow the screen to sleep when not tracking.
        if (Session.Status != SessionStatus.Tracking)
        {
            const int lostTrackingSleepTimeout = 15;
            sleepTimeout = lostTrackingSleepTimeout;
        }
#endif

        Screen.sleepTimeout = sleepTimeout;

        if (m_IsQuitting)
        {
            return;
        }

        // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
        if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
        {
            _ShowAndroidToastMessage("Camera permission is needed to run this application.");
            m_IsQuitting = true;
            Invoke("_DoQuit", 0.5f);
        }
        else if (Session.Status.IsError())
        {
            _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
            m_IsQuitting = true;
            Invoke("_DoQuit", 0.5f);
        }
    }

    /// <summary>
    /// Actually quit the application.
    /// </summary>
    private void _DoQuit()
    {
        Application.Quit();
    }

    /// <summary>
    /// Show an Android toast message.
    /// </summary>
    /// <param name="message">Message string to show in the toast.</param>
    private void _ShowAndroidToastMessage(string message)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                    message, 0);
                toastObject.Call("show");
            }));
        }
    }
}
