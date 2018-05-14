using System;
using System.Collections.Generic;
using GoogleARCore.CrossPlatform;
using UnityEngine;
using UnityEngine.Networking;
using GoogleARCore.Examples.CloudAnchor;

public class RoomServer : MonoBehaviour {

    private Dictionary<int, XPAnchor> m_RoomAnchorsDict = new Dictionary<int, XPAnchor>();

    /// <summary>
    /// Initialize the server.
    /// </summary>
    public void Start()
    {
        NetworkServer.Listen(8888);
        NetworkServer.RegisterHandler(RoomSharingMsgType.AnchorIdFromRoomRequest, OnGetAnchorIdFromRoomRequest);
    }

    /// <summary>
    /// Saves the cloud anchor to room.
    /// </summary>
    /// <param name="room">The room to save the anchor to.</param>
    /// <param name="anchor">The Anchor to save.</param>
    public void SaveCloudAnchorToRoom(int room, XPAnchor anchor)
    {
        m_RoomAnchorsDict.Add(room, anchor);
    }

    /// <summary>
    /// Resolves a room request.
    /// </summary>
    /// <param name="netMsg">The resolve room request.</param>
    private void OnGetAnchorIdFromRoomRequest(NetworkMessage netMsg)
    {
        var roomMessage = netMsg.ReadMessage<AnchorIdFromRoomRequestMessage>();
        XPAnchor anchor;
        bool found = m_RoomAnchorsDict.TryGetValue(roomMessage.RoomId, out anchor);
        AnchorIdFromRoomResponseMessage response = new AnchorIdFromRoomResponseMessage
        {
            Found = found,
        };

        if (found)
        {
            response.AnchorId = anchor.CloudId;
        }

        NetworkServer.SendToClient(netMsg.conn.connectionId, RoomSharingMsgType.AnchorIdFromRoomResponse, response);
    }
}
