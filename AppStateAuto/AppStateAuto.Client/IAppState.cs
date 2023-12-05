﻿namespace AppStateAuto.Client;

public interface IAppState
{
    string Message { get; set; }
    int Count { get; set; }
    DateTime LastStorageSaveTime { get; set; }
}
