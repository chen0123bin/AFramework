using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHotFixCommand {
    void ExecuteAwake();
    void ExecuteStart();

    void ExecuteUpdate();
    void ExecuteOnDestroy();
    void ExecuteOnEnable();
    void ExecuteOnDisable();
}
