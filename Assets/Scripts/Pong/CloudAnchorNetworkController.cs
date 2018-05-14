using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using GoogleARCore;
using GoogleARCore.CrossPlatform;
using GoogleARCore.Examples.CloudAnchor;

public class CloudAnchorNetworkController : NetworkBehaviour
{
	#region Serialized fields

    [SerializeField]
    GameObject _andyPrefab;

	#endregion

	#region SyncVar fields

	[SyncVar(hook = "OnChangeAnchor")]
	bool _anchorFound;

	[SyncVar]
	string _anchorId;

	#endregion

	#region Private fields

	#endregion

	#region Cloud Anchor fields

	/// <summary>
    /// The last placed anchor.
    /// </summary>
    private Component m_LastPlacedAnchor = null;

    /// <summary>
    /// The last resolved anchor.
    /// </summary>
    private XPAnchor m_LastResolvedAnchor = null;

	#endregion

	#region Monobehaviour functions

	private void Start()
	{
		_ResetStatus();

		if (isServer)
		{
		}
		else
		{
			// check if anchor is available
            if (_anchorFound)
			{
				_ResolveAnchorFromId(_anchorId);
			}
		}
    }

	private void Update()
	{
		if (isServer)
		{
			ProcessTouch();
		}
	}

	#endregion

	#region Public functions

	#endregion

	#region Private functions

    private void ProcessTouch()
	{
        // if there's already an anchor, skip
		if (m_LastPlacedAnchor != null)
		{
			return;
		}

		// If the player has not touched the screen then the update is complete.
        Touch touch;
        if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
        {
            return;
        }

		Pose pose;
		Trackable trackable;

		if (ARCoreManager.Instance.RaycastTouch(touch.position.x, touch.position.y, out pose, out trackable))
		{
			m_LastPlacedAnchor = ARCoreManager.Instance.CreateAnchor(pose, trackable);
		}

		if (m_LastPlacedAnchor != null)
		{
			var andy = Instantiate(
				_andyPrefab,
				m_LastPlacedAnchor.transform.position,
				m_LastPlacedAnchor.transform.rotation);

			// Compensate for the hitPose rotation facing away from the raycast (i.e. camera).
            andy.transform.Rotate(0, 180, 0, Space.Self);

            // Make Andy model a child of the anchor.
            andy.transform.parent = m_LastPlacedAnchor.transform;

            // Save cloud anchor.
            _HostLastPlacedAnchor();
		}
	}

	#endregion

	#region SyncVar functions

	void OnChangeAnchor(bool found)
	{
		if (isServer) return;

		if (found)
		{
			Debug.Log("anchor found: " + _anchorId);
			_ResolveAnchorFromId(_anchorId);
		}
	}

	#endregion

    #region Cloud Anchor functions

	/// <summary>
	/// Resets the internal status and UI.
	/// </summary>
	private void _ResetStatus()
    {
        // Reset internal status.
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
		ARCoreManager.Instance.uiController.ShowReadyMode();
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
		ARCoreManager.Instance.uiController.ShowHostingModeAttemptingHost();
        XPSession.CreateCloudAnchor(anchor).ThenAction(result =>
        {
            if (result.Response != CloudServiceResponse.Success)
            {
				ARCoreManager.Instance.uiController.ShowHostingModeBegin(
                    string.Format("Failed to host cloud anchor: {0}", result.Response));
                return;
            }

			_anchorId = result.Anchor.CloudId;
			_anchorFound = true;
			Debug.Log("hosting anchor id " + _anchorId);

			ARCoreManager.Instance.uiController.ShowHostingModeBegin("Cloud anchor was created and saved.");
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
				ARCoreManager.Instance.uiController.ShowResolvingModeBegin(string.Format("Resolving Error: {0}.", result.Response));
                return;
            }

            m_LastResolvedAnchor = result.Anchor;

			Debug.Log("resolved anchor " + m_LastResolvedAnchor);
            
            // do somethign better
            transform.SetPositionAndRotation(result.Anchor.transform.position, result.Anchor.transform.rotation);
            Instantiate(_andyPrefab, result.Anchor.transform);

			ARCoreManager.Instance.uiController.ShowResolvingModeSuccess();
        }));
    }

	#endregion
}
