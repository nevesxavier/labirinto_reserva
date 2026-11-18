using UnityEngine;

/// <summary>
/// Um dos 10 tesouros. É criado pelo GerenciadorJogo; flutua e brilha enquanto espera,
/// e avisa o GerenciadorJogo quando o jogador encosta.
/// </summary>
public class ItemColetavel : MonoBehaviour
{
    // Número do tesouro (0 a 9)
    public int indice;

    private bool coletado;
    private Transform visual;
    private Transform brilho;
    private float defasagem;
    private float tamanho = 1f;

    /// <param name="escala">1 = normal; o tesouro final é maior.</param>
    public void Configurar(int indiceGlobal, int camadaId, int ordem, float escala = 1f)
    {
        indice = indiceGlobal;
        tamanho = escala;
        defasagem = indiceGlobal * 0.7f;

        // Brilho atrás do ícone
        GameObject objBrilho = new GameObject("Brilho");
        objBrilho.transform.SetParent(transform, false);
        SpriteRenderer desenhoBrilho = objBrilho.AddComponent<SpriteRenderer>();
        desenhoBrilho.sprite = Recursos.Circulo();
        Color corBrilho = Recursos.CoresItens[Mathf.Clamp(indiceGlobal, 0, Recursos.CoresItens.Length - 1)];
        corBrilho.a = 0.40f;
        desenhoBrilho.color = corBrilho;
        desenhoBrilho.sortingLayerID = camadaId;
        desenhoBrilho.sortingOrder = ordem - 2;
        brilho = objBrilho.transform;

        // Ícone do tesouro (filho, para flutuar sem mexer no colisor)
        GameObject objVisual = new GameObject("Icone");
        objVisual.transform.SetParent(transform, false);
        objVisual.transform.localScale = new Vector3(1.25f * escala, 1.25f * escala, 1f);
        SpriteRenderer desenho = objVisual.AddComponent<SpriteRenderer>();
        desenho.sprite = Recursos.Icone(indiceGlobal);
        desenho.sortingLayerID = camadaId;
        desenho.sortingOrder = ordem - 1;
        visual = objVisual.transform;

        CircleCollider2D colisor = gameObject.AddComponent<CircleCollider2D>();
        colisor.isTrigger = true;
        colisor.radius = 0.6f * escala;
    }

    private void Update()
    {
        float t = Time.time + defasagem;

        if (visual != null)
            visual.localPosition = new Vector3(0f, Mathf.Sin(t * 3f) * 0.12f, 0f);

        if (brilho != null)
        {
            float tam = (1.9f + Mathf.Sin(t * 4f) * 0.15f) * tamanho;
            brilho.localScale = new Vector3(tam, tam, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (coletado || !outro.CompareTag("Player")) return;
        coletado = true;

        if (GerenciadorJogo.Instancia != null)
            GerenciadorJogo.Instancia.ColetarItem(this);

        Destroy(gameObject);
    }
}
