using UnityEngine;

/// <summary>
/// Vai no portão e no selo do buraco: quando o jogador bate neles enquanto
/// estão trancados, o GerenciadorJogo mostra um aviso dizendo o que falta.
/// tipo 0 = portão | tipo 1 = buraco selado
/// </summary>
public class AvisoBloqueio : MonoBehaviour
{
    public int tipo;

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (!colisao.gameObject.CompareTag("Player")) return;

        if (GerenciadorJogo.Instancia != null)
            GerenciadorJogo.Instancia.AvisarBloqueio(tipo);
    }
}

/// <summary>
/// Um buraco do mapa. O buraco de ENTRADA (canto inferior) começa tampado por um selo de pedra;
/// quando é liberado, brilha e teletransporta o jogador. O buraco de SAÍDA (topo) só ganha um brilho
/// depois que o jogador chega por ele.
/// </summary>
public class PortalBuraco : MonoBehaviour
{
    private bool entrada;
    private bool liberado;

    private int camadaId;
    private int ordem;

    private SpriteRenderer desenhoSelo;
    private GameObject bloqueio;
    private SpriteRenderer desenhoBrilho;
    private CircleCollider2D gatilho;

    private float alfaSelo = 1f;
    private float proximaFaisca;

    public bool Liberado
    {
        get { return liberado; }
    }

    public void Configurar(int camadaId, int ordem, bool entrada, bool comSelo)
    {
        this.camadaId = camadaId;
        this.ordem = ordem;
        this.entrada = entrada;

        // Brilho do portal (aparece quando liberado)
        GameObject objBrilho = new GameObject("Brilho");
        objBrilho.transform.SetParent(transform, false);
        objBrilho.transform.localScale = new Vector3(3.4f, 3.4f, 1f);
        desenhoBrilho = objBrilho.AddComponent<SpriteRenderer>();
        desenhoBrilho.sprite = Recursos.Circulo();
        desenhoBrilho.color = new Color(0.55f, 0.85f, 1f, 0.55f);
        desenhoBrilho.sortingLayerID = camadaId;
        desenhoBrilho.sortingOrder = ordem - 3;
        objBrilho.SetActive(false);

        // Gatilho que teletransporta (só existe no buraco de entrada)
        gatilho = gameObject.AddComponent<CircleCollider2D>();
        gatilho.isTrigger = true;
        gatilho.radius = 0.9f;
        gatilho.enabled = false;

        if (comSelo)
        {
            bloqueio = new GameObject("Selo");
            bloqueio.transform.SetParent(transform, false);

            desenhoSelo = bloqueio.AddComponent<SpriteRenderer>();
            desenhoSelo.sprite = Recursos.Selo();
            desenhoSelo.sortingLayerID = camadaId;
            desenhoSelo.sortingOrder = ordem - 4;

            CircleCollider2D solido = bloqueio.AddComponent<CircleCollider2D>();
            solido.radius = 1.4f;

            AvisoBloqueio aviso = bloqueio.AddComponent<AvisoBloqueio>();
            aviso.tipo = 1;
        }
    }

    /// <summary>Tampa o buraco de novo (ao reiniciar o jogo).</summary>
    public void Selar()
    {
        liberado = false;
        alfaSelo = 1f;

        if (desenhoSelo != null)
        {
            desenhoSelo.color = Color.white;
            bloqueio.SetActive(true);

            CircleCollider2D solido = bloqueio.GetComponent<CircleCollider2D>();
            if (solido != null) solido.enabled = true;
        }

        desenhoBrilho.gameObject.SetActive(false);
        gatilho.enabled = false;
    }

    /// <summary>Tira o selo e acende o brilho.</summary>
    public void Liberar()
    {
        liberado = true;
        desenhoBrilho.gameObject.SetActive(true);
        gatilho.enabled = entrada;

        if (bloqueio != null)
        {
            // O selo some aos poucos; o bloqueio físico sai já
            CircleCollider2D solido = bloqueio.GetComponent<CircleCollider2D>();
            if (solido != null) solido.enabled = false;
        }
    }

    private void Update()
    {
        if (desenhoSelo != null && liberado && alfaSelo > 0f)
        {
            alfaSelo = Mathf.Max(0f, alfaSelo - Time.deltaTime * 1.2f);
            desenhoSelo.color = new Color(1f, 1f, 1f, alfaSelo);
            if (alfaSelo <= 0f) bloqueio.SetActive(false);
        }

        if (!liberado) return;

        float pulso = 3.4f + Mathf.Sin(Time.time * 4f) * 0.3f;
        desenhoBrilho.transform.localScale = new Vector3(pulso, pulso, 1f);

        proximaFaisca -= Time.deltaTime;
        if (proximaFaisca <= 0f)
        {
            proximaFaisca = 0.12f;
            Vector3 ponto = transform.position + new Vector3(Random.Range(-1.1f, 1.1f), Random.Range(-1.1f, 0.6f), 0f);
            Efeitos.Brilho(ponto, new Color(0.65f, 0.9f, 1f), camadaId, ordem);
        }
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (!liberado || !entrada || !outro.CompareTag("Player")) return;

        if (GerenciadorJogo.Instancia != null)
            GerenciadorJogo.Instancia.EntrarNoBuraco();
    }
}
