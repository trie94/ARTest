using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using GoogleARCore.CrossPlatform;
using GoogleARCore.Examples.CloudAnchor;

public class ARCoreManager : GameSingleton<ARCoreManager>
{
	#region Serialized fields

    [SerializeField]
    CloudAnchorUIController _uiController;
	public CloudAnchorUIController uiController
	{
		get { return _uiController; }
	}

    [SerializeField]
    GameObject _arCoreRoot;
    [SerializeField]
    GameObject _arKitRoot;

    /// <summary>
    /// The first-person camera used to render the AR background texture for ARKit.
    /// </summary>
	[SerializeField]
    Camera _arKitFirstPersonCamera;

    #endregion

    #region Cloud Anchor fields

    /// <summary>
    /// True if the app is in the process of quitting due to an ARCore connection error, otherwise false.
    /// </summary>
    private bool m_IsQuitting = false;

	/// <summary>
    /// A helper object to ARKit functionality.
    /// </summary>
	private ARKitHelper _arKitHelper = new ARKitHelper();
	public ARKitHelper arKitHelper
	{
		get { return _arKitHelper; }
	}
    
    #endregion

    #region Monobehaviour functions

    // Use this for initialization
    void Start()
    {
        if (Application.platform != RuntimePlatform.IPhonePlayer)
        {
            _arCoreRoot.SetActive(true);
            _arKitRoot.SetActive(false);
        }
        else
        {
            _arCoreRoot.SetActive(false);
            _arKitRoot.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        _UpdateApplicationLifecycle();      
    }

	#endregion

	#region Public functions

	public bool RaycastTouch(float x, float y, out Pose pose, out Trackable trackable)
	{
		if (Application.platform != RuntimePlatform.IPhonePlayer)
		{
			TrackableHit hit;
			if (Frame.Raycast(x, y, TrackableHitFlags.PlaneWithinPolygon, out hit))
			{
				pose = hit.Pose;
				trackable = hit.Trackable;
				return true;
			}
        }
		else
		{
			Pose hitPose;
            if (_arKitHelper.RaycastPlane(_arKitFirstPersonCamera, x, y, out hitPose))
			{
				pose = hitPose;
				trackable = null;
				return true;
			}
		}
		pose = new Pose();
		trackable = null;
		return false;
	}

	public Component CreateAnchor(Pose pose, Trackable trackable)
    {
        if (Application.platform != RuntimePlatform.IPhonePlayer)
        {
            return trackable.CreateAnchor(pose);
        }
        else
        {
            return _arKitHelper.CreateAnchor(pose);
        }
    }

    #endregion

	#region Cloud Anchor Behaviours

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

    #endregion

}
