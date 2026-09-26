using UnityEngine;

public class Chain : MonoBehaviour
{
    public ChainLink[] chainLinks;

    public Vector2 anchor;

    public float damping;

    public float gravity;

    public float maxTurnSpeed=8f;

    public float targetAngle = 0;



    void Update()
    {

        ChainLink baseLink = chainLinks[0];

        float oldAngle = baseLink.angle;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        Vector2 dir = (Vector2)mouseWorld - anchor;
        float baseLinkTargetAngle = Mathf.Atan2(dir.y, dir.x);

        baseLink.angle = rotateToward(baseLink.angle, baseLinkTargetAngle, maxTurnSpeed * Time.deltaTime);
        baseLink.angVel = (baseLink.angle - oldAngle) / Time.deltaTime;

        for(int i = 1; i < chainLinks.Length; i++)
        {
            ChainLink link = chainLinks[i];
            ChainLink parent = chainLinks[i - 1];

            float diff = wrapAngle(parent.angle - link.angle);
            float accel = diff * link.stiffness - (link.angVel - parent.angVel) * damping + gravity * Mathf.Cos(link.angle);
            link.angVel += accel * Time.deltaTime;
            link.angle += link.angVel * Time.deltaTime;

            chainLinks[0].start = anchor;
            for (int j = 1; j < chainLinks.Length;j++)
            {
                chainLinks[j].start = chainLinks[j - 1].end();
            }

            foreach(ChainLink l in chainLinks)
            {
                l.updateCollider();
            }
        }
    }

    public float wrapAngle(float a)
    {
        return Mathf.Atan2(Mathf.Sin(a), Mathf.Cos(a));
    }

    public float rotateToward(float current, float target, float maxStep)
    {
        float d = wrapAngle(target - current);
        return current + Mathf.Clamp(d, -maxStep, maxStep);

    }


}
