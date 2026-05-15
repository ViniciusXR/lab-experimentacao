using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace Lab03S03.Report;

/// <summary>
/// Textos narrativos do relatório (índice e introdução expandida), alinhados à versão com índice do Lab03.
/// </summary>
internal static class ReportNarrativeContent
{
    internal static void ComposeIndice(ColumnDescriptor col)
    {
        col.Item().Border(1).BorderColor(Colors.Purple.Lighten3).Padding(10).Background(Colors.Purple.Lighten5).Column(box =>
        {
            box.Item().Text("Índice").Bold().FontSize(14).FontColor(Colors.Purple.Darken3);
            box.Item().PaddingTop(4).Text(
                "As páginas são indicadas no rodapé do PDF. Abaixo, a estrutura do documento:").FontSize(9.5f);

            box.Item().PaddingTop(8).Text("1. Introdução").SemiBold();
            box.Item().Text("    1.1 Contexto").FontSize(9.5f);
            box.Item().Text("    1.2 Definição do Problema").FontSize(9.5f);
            box.Item().Text("    1.3 Questões de Pesquisa e Hipóteses").FontSize(9.5f);
            box.Item().Text("    1.4 Objetivo Principal").FontSize(9.5f);

            box.Item().PaddingTop(6).Text("2. Planejamento e execução do experimento").SemiBold();
            box.Item().Text("    2.1 Evidência visual do planejamento — Parte 2").FontSize(9.5f);

            box.Item().PaddingTop(6).Text("3. Metodologia").SemiBold();
            box.Item().Text("    3.1 Criação do Dataset").FontSize(9.5f);
            box.Item().Text("    3.2 Teste Estatístico").FontSize(9.5f);
            box.Item().Text("    3.3 Sumarização dos Resultados").FontSize(9.5f);

            box.Item().PaddingTop(6).Text("4. Resultados (RQs)").SemiBold();
            box.Item().Text("    • RQ01 — Status de aceite vs. Tamanho do PR").FontSize(9.5f);
            box.Item().Text("    • RQ02 — Status de aceite vs. Tempo de análise").FontSize(9.5f);
            box.Item().Text("    • RQ03 — Status de aceite vs. Descrição").FontSize(9.5f);
            box.Item().Text("    • RQ04 — Status de aceite vs. Interações").FontSize(9.5f);
            box.Item().Text("    • RQ05 — Revisões vs. Tamanho do PR").FontSize(9.5f);
            box.Item().Text("    • RQ06 — Revisões vs. Tempo de análise").FontSize(9.5f);
            box.Item().Text("    • RQ07 — Revisões vs. Descrição").FontSize(9.5f);
            box.Item().Text("    • RQ08 — Revisões vs. Interações").FontSize(9.5f);

            box.Item().PaddingTop(6).Text("5. Sumarização global das métricas (processo)").SemiBold();
            box.Item().Text("    5.1 Interpretação (processo)").FontSize(9.5f);

            box.Item().PaddingTop(6).Text("6. Discussão Geral e Conclusão").SemiBold();
        });
    }

    internal static void ComposeIntroducaoCompleta(ColumnDescriptor col, int totalPrs)
    {
        col.Item().Border(1).BorderColor(Colors.Orange.Lighten3).Padding(10).Background(Colors.Orange.Lighten5).Column(box =>
        {
            box.Item().Text("1. Introdução").Bold().FontSize(13).FontColor(Colors.Orange.Darken3);

            box.Item().PaddingTop(6).Text(
                "A revisão de código (code review) é uma etapa indispensável no desenvolvimento de software, " +
                "destacando-se em metodologias ágeis e projetos de código aberto (open source). Seu propósito " +
                "é auditar modificações antes de incorporá-las à base principal do projeto, minimizando a inserção " +
                "de falhas e elevando a qualidade do produto final. No ecossistema do GitHub, essa dinâmica " +
                "ocorre primordialmente através de Pull Requests (PRs). Neles, as submissões são debatidas e " +
                "julgadas por revisores, resultando em sua integração (MERGED) ou rejeição (CLOSED), " +
                "frequentemente com o auxílio de checagens automatizadas. O presente laboratório tem como " +
                "finalidade realizar uma caracterização empírica dessa prática em repositórios de destaque no " +
                "GitHub. Para isso, investiga-se de que maneira fatores como tamanho da alteração, tempo de " +
                "duração, riqueza da descrição e nível de interação afetam o desfecho do PR e a quantidade de " +
                "revisões exigidas, evidenciando o impacto dessas variáveis no esforço analítico e na chance de " +
                "aprovação.").FontSize(10);

            box.Item().PaddingTop(10).Text("1.1 Contexto").SemiBold().FontSize(11);
            box.Item().PaddingTop(2).Text(
                "No GitHub, o PR atua como a unidade central para a análise de code review, pois congrega tanto " +
                "as alterações técnicas quanto o histórico social do processo. Ele funciona, por um lado, como um " +
                "\"pacote de código\" que deve ser validado quanto à sua exatidão e compatibilidade. Por outro, " +
                "age como um fórum de negociação que registra comentários, exigências de correção, " +
                "aprovações e o diálogo entre os envolvidos com diferentes atribuições.").FontSize(10);
            box.Item().PaddingTop(4).Text(
                "A teoria e a prática indicam que os atributos do PR impactam diretamente a carga de trabalho da " +
                "revisão e as chances de aceitação. Modificações extensas costumam exigir mais tempo de " +
                "leitura e aumentam o risco de erros passarem despercebidos, enquanto envios menores tendem " +
                "a ser assimilados rapidamente. O intervalo entre a abertura e o fechamento de um PR pode " +
                "revelar tanto a sua complexidade técnica quanto a necessidade de muito retrabalho. Além disso, " +
                "a qualidade da descrição dita o ritmo da revisão, pois esclarece motivações, limites, riscos e " +
                "métodos de teste, facilitando ou travando o trabalho do revisor. Por fim, o volume de interações " +
                "(como o número de participantes e comentários) aponta para processos mais colaborativos, " +
                "rigorosos ou polêmicos, influenciando o sucesso da integração.").FontSize(10);
            box.Item().PaddingTop(4).Text(
                "Como os repositórios populares variam drasticamente em termos de linguagem, governança, " +
                "políticas de aceite e nível de automação, é crucial validar essas hipóteses intuitivas de forma " +
                "empírica e em larga escala. Dessa forma, caracterizar o code review através de dados extraídos " +
                "de PRs possibilita a identificação objetiva de padrões, gerando insights valiosos para " +
                "colaboradores e mantenedores de projetos.").FontSize(10);

            box.Item().PaddingTop(10).Text("1.2 Definição do Problema").SemiBold().FontSize(11);
            box.Item().PaddingTop(2).Text(
                "O foco deste experimento é investigar a relação entre as características observáveis dos PRs " +
                "revisados e o desfecho do processo de revisão. De forma específica, analisa-se como as " +
                "variáveis de tamanho, duração da análise, detalhamento da descrição e engajamento social " +
                "determinam se um PR será efetivamente incorporado (MERGED) ou encerrado sem aprovação " +
                "(CLOSED).").FontSize(10);
            box.Item().PaddingTop(4).Text(
                "Para além do status final, o estudo também mede o impacto dessas mesmas características na " +
                "intensidade da revisão, utilizando o total de revisões executadas no PR como métrica. Para " +
                "garantir que os dados reflitam predominantemente o esforço humano — e para mitigar o viés de " +
                "encerramentos automatizados —, o conjunto de dados foi filtrado para incluir exclusivamente " +
                "PRs que possuam ao menos uma avaliação registrada e cujo tempo de vida (da abertura ao " +
                "desfecho) ultrapasse uma hora.").FontSize(10);

            box.Item().PaddingTop(10).Text("1.3 Questões de Pesquisa e Hipóteses").SemiBold().FontSize(11);
            box.Item().PaddingTop(2).Text(
                "Abaixo estão as perguntas que norteiam a pesquisa e as hipóteses informais elaboradas a partir " +
                "das expectativas do comportamento em repositórios populares no GitHub.").FontSize(10);
            box.Item().PaddingTop(4).Text(
                "• RQ01 (Tamanho vs Status): Estima-se que PRs com um volume massivo de modificações tenham " +
                "menores chances de aprovação (MERGED). Alterações muito grandes sobrecarregam o revisor " +
                "cognitivamente, elevam a probabilidade de falhas e complicam a análise de impacto, o que geralmente " +
                "resulta em mais exigências de refatoração ou descarte.").FontSize(9.5f);
            box.Item().PaddingTop(3).Text(
                "• RQ02 (Tempo vs Status): PRs que demoram longos períodos em análise tendem a ser finalizados como " +
                "CLOSED. Prazos extensos indicam atritos no processo, fazendo com que o código fique obsoleto frente à " +
                "branch principal (envelhecimento do PR).").FontSize(9.5f);
            box.Item().PaddingTop(3).Text(
                "• RQ03 (Descrição vs Status): Textos descritivos mais robustos (maior body length) aumentam a " +
                "probabilidade de um PR ser MERGED. Explicações minuciosas clarificam a intenção da mudança e " +
                "métodos de teste, mitigando as dúvidas dos revisores.").FontSize(9.5f);
            box.Item().PaddingTop(3).Text(
                "• RQ04 (Interações vs Status): PRs com altos índices de comentários e participantes tendem a ser " +
                "CLOSED. O alto engajamento frequentemente sinaliza polêmicas, identificação de falhas ou longos " +
                "debates sobre arquitetura e estilo, gerando um desgaste que pode culminar na rejeição.").FontSize(9.5f);
            box.Item().PaddingTop(3).Text(
                "• RQ05 (Tamanho vs Revisões): Existe uma correlação positiva entre o tamanho do PR e a quantidade de " +
                "revisões. Submissões extensas naturalmente demandam inspeções mais longas e são mais suscetíveis a " +
                "apontamentos em diferentes módulos do código.").FontSize(9.5f);
            box.Item().PaddingTop(3).Text(
                "• RQ06 (Tempo vs Revisões): Quanto mais revisões um PR sofre, maior o seu tempo total de tramitação. " +
                "Cada novo ciclo exige leitura, debate, codificação de correções e reexecução de testes automatizados " +
                "(CI), alongando o período.").FontSize(9.5f);
            box.Item().PaddingTop(3).Text(
                "• RQ07 (Descrição vs Revisões): Antecipa-se uma correlação negativa, em que descrições mais ricas " +
                "resultam em menos revisões. Quando a motivação e a lógica da mudança são bem expostas desde o " +
                "princípio, o revisor tem menos dúvidas estruturais, encurtando rodadas de feedback.").FontSize(9.5f);
            box.Item().PaddingTop(3).Text(
                "• RQ08 (Interações vs Revisões): Prevê-se uma associação positiva entre as métricas de interação e a " +
                "quantidade de revisões. Conversas e discussões densas geralmente geram a necessidade de ajustes " +
                "complementares. Assim, o feedback resulta em novas validações formais.").FontSize(9.5f);

            box.Item().PaddingTop(10).Text("1.4 Objetivo Principal").SemiBold().FontSize(11);
            box.Item().PaddingTop(2).Text(
                "Realizar uma caracterização empírica da dinâmica de code review em repositórios de grande " +
                "circulação no GitHub. O intuito é mapear como as métricas relativas a tamanho, duração " +
                "temporal, qualidade do texto descritivo e engajamento social estão atreladas ao desfecho do " +
                "processo e à quantidade de revisões recebidas. O fim último da pesquisa é extrair evidências " +
                $"quantitativas consistentes através da modelagem estatística de {totalPrs:N0} PRs.").FontSize(10);
        });
    }
}
