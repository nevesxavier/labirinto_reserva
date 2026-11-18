using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Personagem")]
    public Transform personagem;

    [Header("Suavidade da Câmera")]
    public float suavidade = 3f;

    [Header("Deslocamento da Câmera")]
    public Vector3 deslocamento = new Vector3(0f, 0.5f, -10f);

    void LateUpdate()
    {
        if (personagem == null)
            return;

        Vector3 destino = personagem.position + deslocamento;

        transform.position = Vector3.Lerp(
            transform.position,
            destino,
            Mathf.Clamp01(suavidade * Time.deltaTime)
        );
    }

    /// <summary>Pula a câmera direto para uma posição (usado ao trocar de fase).</summary>
    public void IrPara(Vector2 posicao)
    {
        transform.position = new Vector3(
            posicao.x + deslocamento.x,
            posicao.y + deslocamento.y,
            deslocamento.z);
    }
}
