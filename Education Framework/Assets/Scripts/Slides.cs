using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Slides : NetworkBehaviour
{
    public Texture[] slideTextures;

    public Material material;

    public RawImage rawImage;

    private int _uiSlide;
    public int uiSlide
    {
        get
        {
            return _uiSlide;
        }

        set
        {
            _uiSlide = Math.Min(Math.Max(value, 0), slideTextures.Length - 1);
            rawImage.texture = slideTextures[_uiSlide];

            if (isServer)
            {
                currentSlide = _uiSlide;
            }
        }
    }

    public bool uiOpen
    {
        get
        {
            return rawImage.gameObject.activeSelf;
        }

        set
        {
            rawImage.gameObject.SetActive(value);
            Player.localPlayer.controlsCamera = !value;

            if (value)
            {
                uiSlide = currentSlide;
            }
        }
    }

    [NonSerialized]
    public NetworkIdentity networkIdentity;

    [SyncVar]
    private int _currentSlide;

    public int currentSlide
    {
        get
        {
            return _currentSlide;
        }

        set
        {
            _currentSlide = Math.Min(Math.Max(value, 0), slideTextures.Length - 1);
        }
    }

    public void advanceSlides(int slides)
    {
        //currentSlide += slides;
        uiSlide += slides;
    }

    private void Start()
    {
        networkIdentity = GetComponent<NetworkIdentity>();

        currentSlide = 0;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            uiOpen = !uiOpen;
        }
        
        if (Player.localPlayer == null)
        {
            return;
        }

        if (isServer)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentSlide--;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentSlide++;
            }
        }

        material.mainTexture = slideTextures[currentSlide];
    }
}
