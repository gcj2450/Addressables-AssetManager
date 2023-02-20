using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Asgard;

public class TestAbLoadScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ABResBackDownloader.Instance.InitSys();
        ABResChecker.Instance.InitSys();
        ABResExplorer.Instance.InitSys();
        ABResLoaderManager.Instance.InitSys();
        ABResThreadPoll.Instance.InitSys();

        TestAbLoad.instance.TestGetObj();
    }

    // Update is called once per frame
    void Update()
    {
        int _curTime = (int)(Time.time * 1000);
        int _delta = (int)(Time.deltaTime * 1000);

        ABResBackDownloader.Instance.DoFrameUpdate(_curTime, _delta);
        ABResChecker.Instance.DoFrameUpdate(_curTime, _delta);
        ABResExplorer.Instance.DoFrameUpdate(_curTime, _delta);
        ABResLoaderManager.Instance.DoFrameUpdate(_curTime, _delta);
        ABResThreadPoll.Instance.DoFrameUpdate(_curTime, _delta);
    }
}
