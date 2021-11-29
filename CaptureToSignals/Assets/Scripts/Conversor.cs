using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public struct Signal
{
    public double Start;
    public double End;

    internal string ToCSVEntry()
    {
        return Start.ToString() + "," + End.ToString() + "," + (End - Start).ToString();
    }
}

public class Conversor : MonoBehaviour
{
    public VideoPlayer Player;
    private RenderTexture PlayerRenderTarget;
    private Texture2D PlayerTexture;
    public RawImage PlayerTargetPresenter;

    public Image Signal;
    public Image Comparer;

    public string VideoPath = "";
    public bool DebugMode = false;
    public const float PixelError = 0.02f;

    private bool _signaled;
    private double _signalTime;
    private Color[] _comparer;
    private readonly List<Signal> _collected = new List<Signal>();


    private IEnumerator Start()
    {
        Application.targetFrameRate = 100;
        foreach(var file in Directory.GetFiles(VideoPath))
        {
            if (!file.EndsWith(".mov")) continue;
            Debug.Log("Video:" + file);

            Player.url = "file://" + file;
            Player.Prepare();
            while (!Player.isPrepared) yield return null;
            var size = new Vector2(Player.width, Player.height);

            var args = Path.GetFileNameWithoutExtension(file).Split('_');
            var name = args[0];

            var pos = args[1].Split('x');

            Vector2[] signals;
            if(pos.Length > 2)
            {
                signals = new Vector2[]
                {
                    new Vector2(int.Parse(pos[0]), int.Parse(pos[1])),
                    new Vector2(int.Parse(pos[2]), int.Parse(pos[3]))
                };
            }
            else
            {
                signals = new Vector2[]
                {
                    new Vector2(int.Parse(pos[0]), int.Parse(pos[1]))
                };
            }

            var cl = args[2].Split('x');
            var signal = new Color32(byte.Parse(cl[0]), byte.Parse(cl[1]), byte.Parse(cl[2]), 1);

            var area = args[3].Split('x');
            var comparerRect = new Rect(int.Parse(area[0]), int.Parse(area[1]), int.Parse(area[2]), int.Parse(area[3]));

            var seeker = args[4].Split('x');
            var start = int.Parse(seeker[0]);
            var end = int.Parse(seeker[1]);

            Player.time = 0;
            Player.Prepare();
            while (!Player.isPrepared) yield return null;

            var descriptor = new RenderTextureDescriptor((int)size.x, (int)size.y);
            PlayerRenderTarget = RenderTexture.GetTemporary(descriptor);
            PlayerTexture = new Texture2D((int)size.x, (int)size.y, TextureFormat.ARGB32, false);
            Player.targetTexture = PlayerRenderTarget;
            PlayerTargetPresenter.texture = PlayerTexture;
            PlayerTargetPresenter.rectTransform.sizeDelta = size;
            Player.Play();

            while (Player.time < start)
            {
                UpdateTexture();
                yield return null;
            }

            ProcessClear();
            while (Player.time < end)
            {
                ProcessSignals(signals, signal, comparerRect);
                yield return null;
            }

            if(_collected.Count > 0)
            {
                var content = _collected[0].ToCSVEntry();
                for(int i = 1; i < _collected.Count; i++)
                {
                    content += "\n" + _collected[i].ToCSVEntry();
                }
                File.WriteAllText(VideoPath + name + ".csv", content);
                Debug.Log("Video:" + name + " finished.");
            }
            else
            {
                Debug.Log("Video:" + name + " finished with no entries.");
            }
            Player.Stop();
            PlayerRenderTarget.Release();
            PlayerRenderTarget = null;

            if (DebugMode) break;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ProcessClear()
    {
        _collected.Clear();
        _signaled = false;
        _signalTime = 0;
    }

    private void UpdateTexture()
    {
        var original = RenderTexture.active;
        RenderTexture.active = PlayerRenderTarget;
        PlayerTexture.ReadPixels(new Rect(0, 0, PlayerTexture.width, PlayerTexture.height), 0, 0);
        PlayerTexture.Apply();
        RenderTexture.active = original;
    }

    private bool ProcessSignals(Vector2[] signals, Color signal, Rect area)
    {
        UpdateTexture();

        if (_signaled)
        {
            //Cannot process if time is same!
            if (_signalTime >= Player.time) return false;

            var a = PlayerTexture.GetPixels((int)area.x, (int)area.y, (int)area.width, (int)area.height);
            for(int i = 0; i < a.Length; i++)
            {
                if (!IsApproximated(a[i], _comparer[i]))
                {
                    var delta = Player.time - _signalTime;
                    Debug.Log("Got Signal[" + _signalTime + "][" + Player.time + "] delta:" + delta);
                    _collected.Add(new Signal() { Start = _signalTime, End = Player.time });
                    _signaled = false;
                    return true;
                }
            }
        }
        else
        {
            if(Player.time <= _signalTime + 0.3f) return false;

            foreach(var s in signals)
            {
                var reading = PlayerTexture.GetPixel((int)s.x, (int)s.y);
                if (IsApproximated(reading, signal))
                {
                    Signal.color = reading;
                    _signaled = IsApproximated(reading, signal);
                    _signalTime = Player.time;
                    _comparer = PlayerTexture.GetPixels((int)area.x, (int)area.y, (int)area.width, (int)area.height);
                }
            }
        }
        return false;
    }

    private bool IsApproximated(Color a, Color b)
    {
        var dr = Mathf.Abs(a.r - b.r);
        var dg = Mathf.Abs(a.g - b.g);
        var db = Mathf.Abs(a.b - b.b);
        return dr < PixelError && dg < PixelError && db < PixelError;
    }

    private void OnDestroy()
    {
        if(PlayerRenderTarget != null)
        {
            PlayerRenderTarget.Release();
        }
    }
}
