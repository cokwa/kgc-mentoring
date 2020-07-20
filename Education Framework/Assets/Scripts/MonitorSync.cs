using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

public class MonitorSync : MonoBehaviourPunCallbacks, IPunObservable
{
    [BurstCompile]
    private struct ExtractDisplacementJob : IJobParallelFor
    {
        public NativeArray<Color32> baseImage;
        public NativeArray<Color32> image;

        public void Execute(int index)
        {
            image[index] = new Color32(
                (byte)Math.Min(Math.Max(Mathf.RoundToInt((image[index].r - baseImage[index].r) * 0.5f + 127.5f), 0), 255),
                (byte)Math.Min(Math.Max(Mathf.RoundToInt((image[index].g - baseImage[index].g) * 0.5f + 127.5f), 0), 255),
                (byte)Math.Min(Math.Max(Mathf.RoundToInt((image[index].b - baseImage[index].b) * 0.5f + 127.5f), 0), 255), 255);
        }
    }

    [BurstCompile]
    private struct RecoverJob : IJobParallelFor
    {
        public NativeArray<Color32> baseImage;
        public NativeArray<Color32> image;

        public void Execute(int index)
        {
            image[index] = new Color32(
                (byte)Math.Min(Math.Max(baseImage[index].r + (image[index].r * 2 - 255), 0), 255),
                (byte)Math.Min(Math.Max(baseImage[index].g + (image[index].g * 2 - 255), 0), 255),
                (byte)Math.Min(Math.Max(baseImage[index].b + (image[index].b * 2 - 255), 0), 255), 255);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Color32Array
    {
        [FieldOffset(0)]
        public byte[] byteArray;

        [FieldOffset(0)]
        public Color32[] colors;
    }

    public float sampleRate = 0.1f;
    private float nextSample = 0f;

    public int bufferCount = 10;

    private int currentBufferCount = 0;
    private List<int> segmentSizes = new List<int>();
    private List<byte> buffer = new List<byte>();

    private int currentReadBufferCount = 0;
    private int currentReadBufferOffset = 0;
    private int[] readSegmentSizes;
    private byte[] readBuffer;

    public int quality = 75;
    public int displacementQuality = 5;

    public MeshRenderer screenRenderer;
    private Texture2D screenTexture;

    private uDesktopDuplication.Texture desktopTexture;

    private Color32Array imageBuffer/*, baseImageBuffer*/;
    private NativeArray<Color32> baseImageBuffer;

    public bool mine = false;
    private bool initialized = false;

    [SerializeField]
    private GameObject ui;

    private bool _uiOpen = false;
    public bool uiOpen
    {
        get
        {
            return _uiOpen; 
        }

        set
        {
            _uiOpen = value;

            if (PlayerManager.localPlayer != null)
            {
                PlayerManager.localPlayer.controlsCamera = !_uiOpen;
            }

            ui.SetActive(_uiOpen);
        }
    }

    //private static byte ToByte(int value)
    //{
    //    return (byte)Math.Min(Math.Max(value, 0), 255);
    //}

    //private static byte ToByte(float value)
    //{
    //    return ToByte(Mathf.RoundToInt(value));
    //}

    private void Start()
    {
        screenTexture = new Texture2D(1920, 1080, TextureFormat.BGRA32, false);
        screenTexture.wrapMode = TextureWrapMode.Clamp;

        screenRenderer.material.mainTexture = screenTexture;

        ui.GetComponent<RawImage>().texture = screenTexture;

        imageBuffer = new Color32Array();
        imageBuffer.colors = new Color32[1920 * 1080];

        //baseImageBuffer = new Color32Array();
        //baseImageBuffer.colors = new Color32[1920 * 1080];

        baseImageBuffer = new NativeArray<Color32>(1920 * 1080, Allocator.Persistent);
    }

    private void Update()
    {
        if (mine)
        {
            if (!initialized)
            {
                desktopTexture = GetComponent<uDesktopDuplication.Texture>();
                desktopTexture.enabled = true;
                desktopTexture.monitor.useGetPixels = true;

                GetComponent<ToggleMonitors>().enabled = true;

                initialized = true;
            }

            return;
        }

        if(readSegmentSizes == null || currentReadBufferCount >= readSegmentSizes.Length)
        {
            return;
        }

        if (Time.time < nextSample)
        {
            return;
        }

        nextSample = Time.time + sampleRate;

        byte[] bytes = new byte[readSegmentSizes[currentReadBufferCount]];
        Array.Copy(readBuffer, currentReadBufferOffset, bytes, 0, bytes.Length);

        screenTexture.LoadImage(bytes);

        if (currentReadBufferCount == 0)
        {
            baseImageBuffer.CopyFrom(screenTexture.GetPixels32());
        }
        else
        {
            RecoverJob job = new RecoverJob();
            job.baseImage = baseImageBuffer;
            job.image = new NativeArray<Color32>(screenTexture.GetPixels32(), Allocator.TempJob);

            JobHandle handle = job.Schedule(imageBuffer.colors.Length, 1);
            handle.Complete();

            //for (int i = 0; i < 1920 * 1080; i++)
            //{
            //    imageBuffer.colors[i].r = ToByte(baseImageBuffer.colors[i].r + (imageBuffer.colors[i].r * 2 - 255));
            //    imageBuffer.colors[i].g = ToByte(baseImageBuffer.colors[i].g + (imageBuffer.colors[i].g * 2 - 255));
            //    imageBuffer.colors[i].b = ToByte(baseImageBuffer.colors[i].b + (imageBuffer.colors[i].b * 2 - 255));
            //}

            job.image.CopyTo(imageBuffer.colors);

            screenTexture.SetPixels32(imageBuffer.colors);
            screenTexture.Apply();

            job.image.Dispose();
        }

        currentReadBufferCount++;
        currentReadBufferOffset += bytes.Length;
    }

    public static byte[] Compress(byte[] data)
    {
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
        {
            dstream.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
        MemoryStream input = new MemoryStream(data);
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
        {
            dstream.CopyTo(output);
        }
        return output.ToArray();
    }

    public override void OnCreatedRoom()
    {
        mine = true;
    }

    public override void OnJoinedRoom()
    {
        if (mine)
        {
            photonView.RequestOwnership();
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (Time.time < nextSample)
            {
                return;
            }

            nextSample = Time.time + sampleRate;

            if (!desktopTexture.monitor.useGetPixels)
            {
                desktopTexture.monitor.useGetPixels = true;
            }

            desktopTexture.monitor.GetPixels(imageBuffer.colors, 0, 0, 1920, 1080);

            screenTexture.SetPixels32(imageBuffer.colors);
            screenTexture.Apply();
            
            byte[] bytes;

            if (currentBufferCount == 0)
            {
                baseImageBuffer.CopyFrom(imageBuffer.colors);
                
                bytes = ImageConversion.EncodeArrayToJPG(imageBuffer.byteArray, GraphicsFormat.B8G8R8A8_SRGB, 1920, 1080, 0, quality);
            }
            else
            {
                ExtractDisplacementJob job = new ExtractDisplacementJob();
                job.baseImage = baseImageBuffer;
                job.image = new NativeArray<Color32>(imageBuffer.colors, Allocator.TempJob);

                JobHandle handle = job.Schedule(imageBuffer.colors.Length, 1);
                handle.Complete();

                //for (int i = 0; i < 1920 * 1080; i++)
                //{
                //    imageBuffer.colors[i].r = ToByte((imageBuffer.colors[i].r - baseImageBuffer.colors[i].r) * 0.5f + 127.5f);
                //    imageBuffer.colors[i].g = ToByte((imageBuffer.colors[i].g - baseImageBuffer.colors[i].g) * 0.5f + 127.5f);
                //    imageBuffer.colors[i].b = ToByte((imageBuffer.colors[i].b - baseImageBuffer.colors[i].b) * 0.5f + 127.5f);
                //}

                job.image.CopyTo(imageBuffer.colors);

                bytes = ImageConversion.EncodeArrayToJPG(imageBuffer.byteArray, GraphicsFormat.B8G8R8A8_SRGB, 1920, 1080, 0, displacementQuality);

                job.image.Dispose();
            }

            segmentSizes.Add(bytes.Length);
            buffer.AddRange(bytes);

            if (++currentBufferCount == bufferCount)
            {
                stream.SendNext(segmentSizes.ToArray());
                stream.SendNext(Compress(buffer.ToArray()));

                currentBufferCount = 0;
                segmentSizes.Clear();
                buffer.Clear();
            }
        }
        else
        {
            nextSample = 0f;

            currentReadBufferCount = 0;
            currentReadBufferOffset = 0;
            readSegmentSizes = (int[])stream.ReceiveNext();
            readBuffer = Decompress((byte[])stream.ReceiveNext());
        }
    }
}