using UnityEngine;
using TMPro;

/// <summary>
/// LEGADO. A contagem de itens agora é feita pelo GerenciadorJogo (que também desenha
/// a interface). Este componente continua na cena só para não gerar "script ausente":
/// ele esconde o texto antigo "Itens: x / 10", que ficaria duplicado com o novo HUD.
/// Você pode apagar o objeto "GerenciadorColetaveis" e o "TextoColetaveis" da cena.
/// </summary>
public class GerenciadorColetaveis : MonoBehaviour
{
    public static GerenciadorColetaveis Instancia;

    [Header("Configurações (legado)")]
    public TMP_Text textoColetaveis;

    public int quantidadeColetada = 0;
    public int quantidadeTotal = 10;

    private void Awake()
    {
        Instancia = this;

        if (textoColetaveis != null)
            textoColetaveis.gameObject.SetActive(false);
    }
}
