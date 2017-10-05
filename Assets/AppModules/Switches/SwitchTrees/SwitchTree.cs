using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class SwitchTreeNodeRef {
    SwitchTreeNode node;

    public SwitchTreeNodeRef(SwitchTreeNode node) {
      this.node = node;
    }
  }

  public struct SwitchTreeNode {
    Transform       transform;
    IPropertySwitch objSwitch;

    SwitchTreeNodeRef     parent;
    Stack<SwitchTreeNode> children;

    public int numChildren { get { return children.Count; } }

    public SwitchTreeNode(Transform transform,
                          IPropertySwitch objSwitch,
                          SwitchTreeNodeRef parent = null) {
      this.transform = transform;
      this.objSwitch = objSwitch;

      this.parent = parent;

      children = null;
      constructChildren();
    }

    private void constructChildren() {
      var stack = Pool<Stack<Transform>>.Spawn();
      stack.Clear();
      stack.Push(this.transform);
      try {
        while (stack.Count > 0) {
          var transform = stack.Pop();

          foreach (var child in transform.GetChildren()) {
            // Ignore SwitchTreeControllers, which will handle their own internal
            // hierarchy.
            if (child.GetComponent<SwitchTreeController>() != null) continue;

            // ObjectSwitches get priority, but any Switch will do.
            IPropertySwitch objSwitch = child.GetComponent<ObjectSwitch>();
            if (objSwitch == null) {
              objSwitch = child.GetComponent<IPropertySwitch>();
            }
            if (objSwitch != null) {
              // Each child with a switch component gets a node with the current node as
              // its parent.
              children.Push(new SwitchTreeNode(child,
                                               objSwitch,
                                               new SwitchTreeNodeRef(this)));
            }
            else {
              // This node will "inherit" any grand-children nodes whose parents are
              // not _themselves_ switches as direct "children" in the switch tree.
              stack.Push(child);
            }
          }
        }
      }
      finally {
        stack.Clear();
        Pool<Stack<Transform>>.Recycle(stack);
      }
    }
  }

  [System.Serializable]
  public class SwitchTree {

    [SerializeField]
    private SwitchTreeNode root;

    public SwitchTree(Transform transform) {
      var objSwitch = transform.GetComponent<IPropertySwitch>();
      if (objSwitch == null) {
        throw new System.InvalidOperationException("Cannot build a Switch Tree for "
                                                 + "a Transform that is not itself a "
                                                 + "switch.");
      }

      root = new SwitchTreeNode(transform, objSwitch, null);
    }

    public int NodeCount {
      get { return root.numChildren + 1; }
    }
  }

}