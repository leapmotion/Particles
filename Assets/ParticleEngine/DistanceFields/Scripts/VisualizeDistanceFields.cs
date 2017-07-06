using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

[ExecuteInEditMode]
public class VisualizeDistanceFields : MonoBehaviour {

  public Material effectMat;

  static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr) {
    RenderTexture.active = dest;

    fxMaterial.SetTexture("_MainTex", source);

    GL.PushMatrix();
    GL.LoadOrtho(); // Note: z value of vertices don't make a difference because we are using ortho projection

    fxMaterial.SetPass(passNr);

    GL.Begin(GL.QUADS);

    // Here, GL.MultitexCoord2(0, x, y) assigns the value (x, y) to the TEXCOORD0 slot in the shader.
    // GL.Vertex3(x,y,z) queues up a vertex at position (x, y, z) to be drawn.  Note that we are storing
    // our own custom frustum information in the z coordinate.
    GL.MultiTexCoord2(0, 0.0f, 0.0f);
    GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

    GL.MultiTexCoord2(0, 1.0f, 0.0f);
    GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

    GL.MultiTexCoord2(0, 1.0f, 1.0f);
    GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

    GL.MultiTexCoord2(0, 0.0f, 1.0f);
    GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

    GL.End();
    GL.PopMatrix();
  }

  public float strengthToRadius = 1;
  public float baseRadius = 0.1f;
  public float palmRadius = 1;

  Camera.StereoscopicEye eye = Camera.StereoscopicEye.Left;
  void Update() {
    eye = Camera.StereoscopicEye.Left;

    if(Hands.Left != null) {
      float radius = strengthToRadius / Hands.Left.GrabAngle;
      radius = Mathf.Min(0.25f, radius);

      Vector3 spherePos = Hands.Left.PalmPosition.ToVector3() + Hands.Left.PalmarAxis() * radius;

      effectMat.SetVector("_LeftSpherePos", spherePos);
      effectMat.SetFloat("_LeftSphereRadius", radius);
      effectMat.SetVector("_LeftPalmPos", Hands.Left.PalmPosition.ToVector3());
      effectMat.SetFloat("_LeftPalmRadius", baseRadius + Hands.Left.GrabAngle * palmRadius);
    }

    if (Hands.Right != null) {
      float radius = strengthToRadius / Hands.Right.GrabAngle;
      radius = Mathf.Min(0.25f, radius);

      Vector3 spherePos = Hands.Right.PalmPosition.ToVector3() + Hands.Right.PalmarAxis() * radius;

      effectMat.SetVector("_RightSpherePos", spherePos);
      effectMat.SetFloat("_RightSphereRadius", radius);
      effectMat.SetVector("_RightPalmPos", Hands.Right.PalmPosition.ToVector3());
      effectMat.SetFloat("_RightPalmRadius", baseRadius + Hands.Right.GrabAngle * palmRadius);
    }

  }

  [ImageEffectOpaque]
  void OnRenderImage(RenderTexture source, RenderTexture destination) {
    var camera = GetComponent<Camera>();

    // pass frustum rays to shader
    effectMat.SetMatrix("_FrustumCornersES", GetFrustumCorners(camera));
    effectMat.SetMatrix("_CameraInvViewMatrix", camera.GetStereoViewMatrix(eye).inverse);
    effectMat.SetVector("_CameraWS", camera.GetStereoViewMatrix(eye).inverse.MultiplyPoint3x4(Vector3.zero));
    effectMat.SetMatrix("_FrustumCornersES", GetFrustumCorners(camera));
    CustomGraphicsBlit(source, destination, effectMat, 0);

    eye = Camera.StereoscopicEye.Right;
  }

  private Matrix4x4 GetFrustumCorners(Camera cam) {
    float camFov = cam.fieldOfView;
    float camAspect = cam.aspect;

    Matrix4x4 frustumCorners = Matrix4x4.identity;

    float fovWHalf = camFov * 0.5f;

    float tan_fov = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

    Vector3 toRight = Vector3.right * tan_fov * camAspect;
    Vector3 toTop = Vector3.up * tan_fov;

    Vector3 topLeft = (-Vector3.forward - toRight + toTop);
    Vector3 topRight = (-Vector3.forward + toRight + toTop);
    Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
    Vector3 bottomLeft = (-Vector3.forward - toRight - toTop);

    //topLeft *= 0.5f;
    //topRight *= 0.5f;
    //bottomLeft *= 0.5f;
    //bottomRight *= 0.5f;

    frustumCorners.SetRow(0, topLeft);
    frustumCorners.SetRow(1, topRight);
    frustumCorners.SetRow(2, bottomRight);
    frustumCorners.SetRow(3, bottomLeft);

    return frustumCorners;
  }
}
