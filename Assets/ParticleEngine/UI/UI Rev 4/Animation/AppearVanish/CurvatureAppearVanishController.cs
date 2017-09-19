using Leap.Unity;
using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using Leap.Unity.Space;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CurvatureAppearVanishController : TweenAppearVanishController {

  [Header("Curvature Control")]
  public LeapRadialSpace leapRadialSpace;
  
  [Tooltip("The radius of the space when the object is fully visible.")]
  public float apparentRadius = 1f;

  [Tooltip("The radius of the space when the object is fully invisible.")]
  public float vanishedRadius = 0.10f;

  [UnitCurve]
  public AnimationCurve radiusUnitCurve = DefaultCurve.SigmoidUp;

  protected override void updateAppearVanish(float time, bool immediately = false) {
    leapRadialSpace.radius = radiusUnitCurve.Evaluate(time)
                                            .Map(0f, 1f, vanishedRadius, apparentRadius);
  }

}
