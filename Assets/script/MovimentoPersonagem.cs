using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovimentoPersonagem : MonoBehaviour
{
    [Header("Movimentação")]
    public float velocidade = 7f;
    public float multiplicadorCorrida = 1.6f;   // segure Shift
    public float aceleracao = 70f;

    /// <summary>Quando verdadeiro o personagem fica parado (menus, fim de fase).</summary>
    public bool Congelado { get; set; }

    // Camada de desenho do personagem (os itens usam a mesma, logo abaixo dele)
    public int CamadaOrdenacaoId { get; private set; }
    public int OrdemOrdenacao { get; private set; }

    private Rigidbody2D rb;
    private Vector2 entrada;
    private bool correndo;

    private Transform visual;
    private SpriteRenderer desenhoVisual;
    private float relogioAnimacao;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        CriarVisual();
    }

    // O desenho vira um objeto filho: assim ele pode "pular" e achatar sem
    // mexer no colisor, que fica no objeto principal.
    private void CriarVisual()
    {
        SpriteRenderer original = GetComponent<SpriteRenderer>();
        if (original == null) return;

        CamadaOrdenacaoId = original.sortingLayerID;
        OrdemOrdenacao = original.sortingOrder;

        GameObject go = new GameObject("Visual");
        go.transform.SetParent(transform, false);

        desenhoVisual = go.AddComponent<SpriteRenderer>();
        desenhoVisual.sprite = original.sprite;
        desenhoVisual.color = original.color;
        desenhoVisual.sharedMaterial = original.sharedMaterial;
        desenhoVisual.sortingLayerID = original.sortingLayerID;
        desenhoVisual.sortingOrder = original.sortingOrder;

        original.enabled = false;
        visual = go.transform;
    }

    void Update()
    {
        Vector2 direcao = Vector2.zero;

        if (!Congelado)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) direcao.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) direcao.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) direcao.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) direcao.x += 1f;
        }

        entrada = direcao.sqrMagnitude > 1f ? direcao.normalized : direcao;
        correndo = !Congelado && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        AnimarVisual();
    }

    void FixedUpdate()
    {
        float velocidadeAlvo = velocidade * (correndo ? multiplicadorCorrida : 1f);
        Vector2 alvo = entrada * velocidadeAlvo;

        // Acelera/desacelera suavemente a partir da velocidade real (respeita as paredes)
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, alvo, aceleracao * Time.fixedDeltaTime);
    }

    private void AnimarVisual()
    {
        if (visual == null) return;

        bool andando = rb.linearVelocity.sqrMagnitude > 0.5f;
        relogioAnimacao += Time.deltaTime * (andando ? (correndo ? 17f : 13f) : 2.5f);
        float s = Mathf.Sin(relogioAnimacao);

        if (andando)
        {
            visual.localPosition = new Vector3(0f, Mathf.Abs(s) * 0.10f, 0f);
            visual.localScale = new Vector3(1f + 0.04f * s, 1f - 0.04f * s, 1f);
        }
        else
        {
            visual.localPosition = new Vector3(0f, s * 0.015f, 0f);
            visual.localScale = new Vector3(1f, 1f + 0.015f * s, 1f);
        }

        // Vira o desenho para o lado em que anda
        if (desenhoVisual != null && Mathf.Abs(entrada.x) > 0.1f)
            desenhoVisual.flipX = entrada.x < 0f;
    }
}
