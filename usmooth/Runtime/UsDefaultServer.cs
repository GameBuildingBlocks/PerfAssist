using UnityEngine;

public class UsDefaultServer : MonoBehaviour 
{
    public bool LogRemotely = true;
    public bool LogIntoFile = false;
    public bool InGameGui = false;
    void Awake()
    {
        _usmooth = new UsMain(LogRemotely, LogIntoFile, InGameGui);
    }

	void Start() 
    {
        //        _usmooth = new UsMain(LogRemotely, LogIntoFile, InGameGui);
    }

    void Update()
    {
        if (_usmooth != null)
		    _usmooth.Update();
	}

    void OnDestroy()
    {
        if (_usmooth != null)
            _usmooth.Dispose();
    }

    void OnGUI() 
    {
        if (_usmooth != null)
            _usmooth.OnGUI();
	}

    void OnLevelWasLoaded()
    {
        if (_usmooth != null)
            _usmooth.OnLevelWasLoaded();
    }

    private UsMain _usmooth;
}
