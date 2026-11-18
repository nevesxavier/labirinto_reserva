using UnityEngine;

/// <summary>Partícula simples: voa, encolhe e some.</summary>
public class ParticulaSimples : MonoBehaviour
{
    public Vector2 velocidade;
    public float gravidade;
    public float vida = 0.5f;

    private float idade;
    private Vector3 escalaInicial;
    private SpriteRenderer desenho;
    private Color corInicial;

    private void Start()
    {
        escalaInicial = transform.localScale;
        desenho = GetComponent<SpriteRenderer>();
        if (desenho != null) corInicial = desenho.color;
    }

    private void Update()
    {
        idade += Time.deltaTime;
        if (idade >= vida)
        {
            Destroy(gameObject);
            return;
        }

        velocidade.y -= gravidade * Time.deltaTime;
        transform.position += new Vector3(velocidade.x, velocidade.y, 0f) * Time.deltaTime;

        float restante = 1f - idade / vida;
        transform.localScale = escalaInicial * restante;

        if (desenho != null)
        {
            Color c = corInicial;
            c.a = restante;
            desenho.color = c;
        }
    }
}

public static class Efeitos
{
    /// <summary>Explosão de faíscas ao pegar um tesouro.</summary>
    public static void Explosao(Vector3 centro, Color cor, int camadaId, int ordem)
    {
        Sprite quadrado = Recursos.Quadrado();
        const int quantidade = 16;

        for (int i = 0; i < quantidade; i++)
        {
            GameObject go = new GameObject("Faisca");
            go.transform.position = centro;

            float escala = Random.Range(0.8f, 1.6f);
            go.transform.localScale = new Vector3(escala, escala, 1f);

            SpriteRenderer desenho = go.AddComponent<SpriteRenderer>();
            desenho.sprite = quadrado;
            desenho.color = (i % 3 == 0) ? Color.white : cor;
            desenho.sortingLayerID = camadaId;
            desenho.sortingOrder = ordem + 5;

            float angulo = (i / (float)quantidade) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            float forca = Random.Range(2.5f, 5.5f);

            ParticulaSimples particula = go.AddComponent<ParticulaSimples>();
            particula.velocidade = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * forca;
            particula.gravidade = 4f;
            particula.vida = Random.Range(0.35f, 0.65f);
        }
    }

    /// <summary>Faísca que sobe devagar (brilho dos portais e do portão).</summary>
    public static void Brilho(Vector3 posicao, Color cor, int camadaId, int ordem)
    {
        GameObject go = new GameObject("Brilho");
        go.transform.position = posicao;

        float escala = Random.Range(0.8f, 1.5f);
        go.transform.localScale = new Vector3(escala, escala, 1f);

        SpriteRenderer desenho = go.AddComponent<SpriteRenderer>();
        desenho.sprite = Recursos.Quadrado();
        desenho.color = cor;
        desenho.sortingLayerID = camadaId;
        desenho.sortingOrder = ordem + 5;

        ParticulaSimples particula = go.AddComponent<ParticulaSimples>();
        particula.velocidade = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(1.2f, 2.4f));
        particula.gravidade = 0f;
        particula.vida = Random.Range(0.6f, 1.0f);
    }
}
