using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Match3Tile : MonoBehaviour
{
    public PieceShape shape;
    public int x;
    public int y;

    private Vector3 baseScale;
    private Renderer[] renderers;

    public void Init(int boardX, int boardY, PieceShape pieceShape)
    {
        x = boardX;
        y = boardY;
        shape = pieceShape;
        baseScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>();
        SetSelected(false);
    }

    public void SetBoardPosition(int boardX, int boardY)
    {
        x = boardX;
        y = boardY;
    }

    public void SetSelected(bool selected)
    {
        transform.localScale = selected ? baseScale * 1.1f : baseScale;

        if (renderers == null)
            renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            if (r.material.HasProperty("_EmissionColor"))
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", selected ? Color.white * 0.22f : Color.black);
            }
        }
    }
}
