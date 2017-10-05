using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {
  
  public class SwitchTreeController : ObjectSwitch {
    
    [SerializeField]
    //[SwitchTreeView]
    private SwitchTree tree;

    #region Unity Events

    protected override void Reset() {
      base.Reset();

      initialize();
    }

    protected override void OnValidate() {
      base.OnValidate();

      initialize();
    }

    protected override void Start() {
      base.Start();

      initialize();
    }

    private void initialize() {
      refreshTree();
    }

    private void refreshTree() {
      tree = new SwitchTree(this.transform);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Traverses the tree, deactivating all switch pathways that do not lead to the
    /// switch node identified by this nodeName, then activating all switches along the
    /// path to the switch node identified by this nodeName, but no deeper. Switches that
    /// are children of the named node are also deactivated.
    /// </summary>
    public void SwitchTo(string nodeName) {
      tree.SwitchTo(nodeName);
    }

    #endregion

  }


}
