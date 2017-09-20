using UnityEngine;
using System;
using System.Reflection;

namespace Leap.Unity.DevGui {
  using Attributes;
  using Query;

  public interface IFieldDev {
    DevElement TryBuildDevElement(FieldInfo info);
  }

  public interface IPropertyDev {
    DevElement TryBuildDevElement(PropertyInfo info);
  }

  public interface IMethodDev {
    DevElement TryBuildDevElement(MethodInfo info);
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public class DevCategoryAttribute : Attribute {
    public string category;

    public DevCategoryAttribute(string category) {
      this.category = category;
    }
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public class DevGroupedAttribute : Attribute { }

  public abstract class DevValueAttributeBase : Attribute {
    public string name;
    public string tooltip;

    public string CalcName(MemberInfo info) {
      if (string.IsNullOrEmpty(name)) {
        return Utils.GenerateNiceName(info.Name);
      } else {
        return name;
      }
    }

    public string CalcTooltip(MemberInfo info) {
      if (string.IsNullOrEmpty(tooltip)) {
        var tooltips = info.GetCustomAttributes(typeof(TooltipAttribute), inherit: true);
        if (tooltips.Length > 0) {
          return ((TooltipAttribute)tooltips[0]).tooltip;
        }
      }

      return tooltip;
    }
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
  public class DevValueAttribute : DevValueAttributeBase, IFieldDev, IPropertyDev {
    public DevValueAttribute() { }

    public DevValueAttribute(string name) {
      this.name = name;
    }

    public DevValueAttribute(string name, string tooltip) {
      this.name = name;
      this.tooltip = tooltip;
    }

    public DevElement TryBuildDevElement(FieldInfo info) {
      DevElement element = tryBuildElement(info,
                                           info.FieldType,
                                           (o) => info.GetValue(o),
                                           (o, v) => info.SetValue(o, v));

      if (element != null) {
        element.name = CalcName(info);
        element.tooltip = CalcTooltip(info);
      }

      return element;
    }

    public DevElement TryBuildDevElement(PropertyInfo info) {
      if (info.GetGetMethod(nonPublic: true) == null ||
          info.GetSetMethod(nonPublic: true) == null) {
        Debug.LogError("A property taggetd with DevValue must have both a get and a set method.");
        return null;
      }

      DevElement element = tryBuildElement(info,
                                           info.PropertyType,
                                           (o) => info.GetValue(o, null),
                                           (o, v) => info.SetValue(o, v, null));

      if (element != null) {
        element.name = CalcName(info);
        element.tooltip = CalcTooltip(info);
      }

      return element;
    }

    private DevElement tryBuildElement(MemberInfo info, Type type, Func<object, object> getter, Action<object, object> setter) {
      var rangeAtt = info.GetCustomAttributes(typeof(RangeAttribute), inherit: true).Query().
                                                                                     Cast<RangeAttribute>().
                                                                                     FirstOrNone();

      var minAtt = info.GetCustomAttributes(typeof(MinValue), inherit: true).Query().
                                                                             Cast<MinValue>().
                                                                             FirstOrNone();

      var maxAtt = info.GetCustomAttributes(typeof(MaxValue), inherit: true).Query().
                                                                             Cast<MaxValue>().
                                                                             FirstOrNone();

      var minValue = minAtt.Query().
                            Select(t => t.minValue).
                            Concat(rangeAtt.Query().
                                            Select(t => t.min)).
                            FirstOrNone();

      var maxValue = maxAtt.Query().
                            Select(t => t.maxValue).
                            Concat(rangeAtt.Query().
                                            Select(t => t.max)).
                            FirstOrNone();

      if (type == typeof(int)) {
        return new DevElementInt() {
          min = minValue.Query().Select(Mathf.RoundToInt).FirstOrNone(),
          max = maxValue.Query().Select(Mathf.RoundToInt).FirstOrNone(),
          getValue = (o) => (int)getter(o),
          setValue = (o, i) => setter(o, i)
        };
      } else if (type == typeof(float)) {
        return new DevElementFloat() {
          min = minValue,
          max = maxValue,
          getValue = (o) => (float)getter(o),
          setValue = (o, f) => setter(o, f)
        };
      } else if (type == typeof(string)) {
        return new DevElementString() {
          getValue = (o) => (string)getter(o),
          setValue = (o, s) => setter(o, s)
        };
      } else if (type == typeof(bool)) {
        return new DevElementBool() {
          getValue = (o) => (bool)getter(o),
          setValue = (o, b) => setter(o, b)
        };
      } else if (type.IsEnum) {
        return new DevElementEnum(type) {
          getValue = (o) => (Enum)getter(o),
          setValue = (o, e) => setter(o, e)
        };
      }

      Debug.LogError("DevValue only supports properties of type int, float, string, or bool.");
      return null;
    }
  }

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public class DevButtonAttribute : DevValueAttributeBase, IMethodDev {

    public DevButtonAttribute() { }

    public DevButtonAttribute(string name) {
      this.name = name;
    }

    public DevButtonAttribute(string name, string tooltip) {
      this.name = name;
      this.tooltip = tooltip;
    }

    public DevElement TryBuildDevElement(MethodInfo info) {
      if (info.GetParameters().Length != 0) {
        Debug.LogError("A method tagged with DevButton must have zero parameters.");
        return null;
      }

      if (info.ContainsGenericParameters) {
        Debug.LogError("A method tagged with DevButton must not have any unbound generic parameters.");
        return null;
      }

      return new DevElementButton() {
        name = CalcName(info),
        tooltip = CalcTooltip(info),
        onPress = o => info.Invoke(o, null)
      };
    }
  }
}
