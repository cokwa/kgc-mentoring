using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AutoConnect : MonoBehaviour
{
    public bool server = false;

    private bool initialized = false;
    private bool joined = false;

    private NetworkManager manager;

    private void Start()
    {
        manager = NetworkManager.singleton;
    }

    private void Update()
    {
        if (!initialized)
        {
            manager.StartMatchMaker();

            if (server)
            {
                manager.matchMaker.CreateMatch(manager.matchName, manager.matchSize, true, "", "", "", 0, 0, manager.OnMatchCreate);
            }
            else
            {
                manager.matchMaker.ListMatches(0, 20, "", false, 0, 0, manager.OnMatchList);
            }

            initialized = true;
        }

        if (!server)
        {
            if (!joined && manager.matchInfo == null && manager.matches != null && manager.matches.Count > 0)
            {
                var match = manager.matches[0];

                manager.matchName = match.name;
                manager.matchMaker.JoinMatch(match.networkId, "", "", "", 0, 0, manager.OnMatchJoined);

                joined = true;
            }
        }
    }
}
