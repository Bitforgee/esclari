import hashlib
import os
import re
import struct
import subprocess
import sys
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[3]
FEATURE = Path(__file__).resolve().parent
CONCEITOS = FEATURE / "conceitos"
PESQUISA = (FEATURE / "pesquisa.md").read_text(encoding="utf-8")
FINAIS = [
    "fonte-embutida-symbol.svg",
    "fonte-embutida-lockup.svg",
    "manicula-symbol.svg",
    "manicula-lockup.svg",
    "fio-procedencia-symbol.svg",
    "fio-procedencia-lockup.svg",
]
DIRECOES = ["Fonte embutida", "Manícula contemporânea", "Fio de procedência"]
SLUGS = ["fonte-embutida", "manicula", "fio-procedencia"]


def texto(caminho):
    return caminho.read_text(encoding="utf-8")


def secao(conteudo, titulo):
    trecho = conteudo.split(titulo, 1)[1]
    return trecho.split("\n## ", 1)[0]


def dimensoes_png(caminho):
    dados = caminho.read_bytes()[:24]
    if dados[:8] != b"\x89PNG\r\n\x1a\n":
        raise AssertionError(f"{caminho.name} não é PNG")
    return struct.unpack(">II", dados[16:24])


class ConceitosTests(unittest.TestCase):
    def test_C1_estrategia_tem_dez_territorios_e_tres_direcoes(self):
        territorios = re.findall(r"^\d+\. \*\*.+?\*\*", secao(PESQUISA, "## Territórios da primeira rodada"), re.M)
        self.assertEqual(10, len(territorios), territorios)
        for tipo in ["palavra-marca", "letra-símbolo", "pictorial", "emblema", "combinação", "abstrata"]:
            self.assertIn(tipo, PESQUISA.lower())
        escolhidas = secao(PESQUISA, "## Três direções para construir")
        for direcao in DIRECOES:
            self.assertIn(direcao, escolhidas)

    def test_C2_cliches_proibidos_estao_enumerados(self):
        proibidos = secao(PESQUISA, "### Clichês proibidos")
        grupos = [
            ["balão de conversa", "três pontos"],
            ["robô", "rosto de bot", "headset"],
            ["faísca", "estrela de quatro pontas"],
            ["cérebro", "circuito", "rede de nós", "orbe"],
            ["capelo", "livro aberto", "diploma", "lápis", "lâmpada", "louros", "brasão", "coluna clássica"],
            ["escudo com check", "seta ascendente", "elo", "ponte genérica"],
            ["inicial dentro de quadrado arredondado"],
            ["monograma feito com fonte pronta"],
            ["sans geométrica minúscula com ponto colorido"],
            ["azul mais amarelo como única prova"],
            ["chaves e colchetes"],
            ["gradiente violeta-ciano"],
            ["avatares humano/IA"],
        ]
        for grupo in grupos:
            for item in grupo:
                self.assertIn(item, proibidos, item)

    def test_C3_tres_ideias_funcionam_sem_cor(self):
        refinamentos = texto(FEATURE / "refinamentos.md")
        for direcao in DIRECOES:
            bloco = secao(refinamentos, f"### {direcao}")
            linha = re.search(r"^Ideia monocromática: (.+)$", bloco, re.M)
            self.assertIsNotNone(linha, direcao)
            self.assertIn("clareza com procedência", linha.group(1).lower())
            self.assertNotRegex(linha.group(1).lower(), r"azul|amarel|\bcor(?:es)?\b")

    def test_C4_existem_tres_pares_e_tipos_distintos(self):
        existentes = sorted(p.name for p in CONCEITOS.glob("*.svg") if not re.search(r"-v[12]\.svg$", p.name))
        self.assertEqual(sorted(FINAIS), existentes)
        refinamentos = texto(FEATURE / "refinamentos.md")
        esperados = {
            "Fonte embutida": "wordmark-led combination",
            "Manícula contemporânea": "pictorial combination",
            "Fio de procedência": "abstract combination",
        }
        for direcao, tipo in esperados.items():
            self.assertIn(f"Tipo: {tipo}", secao(refinamentos, f"### {direcao}"))

    def test_C5_svg_final_e_vetorial_monocromatico(self):
        proibidos = ["<text", "<image", "raster", "filter", "mask", "gradient", "font-family", "url("]
        for nome in FINAIS:
            caminho = CONCEITOS / nome
            conteudo = texto(caminho)
            raiz = ET.fromstring(conteudo)
            viewbox = [float(v) for v in raiz.attrib["viewBox"].split()]
            self.assertEqual(4, len(viewbox), nome)
            self.assertTrue(all(v.is_integer() for v in viewbox), nome)
            if "symbol" in nome:
                self.assertEqual([0, 0, 256, 256], [int(v) for v in viewbox], nome)
            else:
                self.assertEqual(256, int(viewbox[3]), nome)
            self.assertIn("<path", conteudo, nome)
            for proibido in proibidos:
                self.assertNotIn(proibido.lower(), conteudo.lower(), f"{nome}: {proibido}")
            cores = re.findall(r"(?:fill|stroke)=[\"']([^\"']+)", conteudo, re.I)
            self.assertTrue(cores, nome)
            for cor in cores:
                self.assertIn(cor.lower(), ["#000", "#000000", "none"], f"{nome}: {cor}")

    def test_C6_auditoria_aprova_os_seis_svg(self):
        auditor = Path.home() / ".kiro/skills/logo-design/scripts/svg_audit.py"
        refinamentos = texto(FEATURE / "refinamentos.md")
        for nome in FINAIS:
            resultado = subprocess.run([sys.executable, str(auditor), str(CONCEITOS / nome)], capture_output=True, text=True, encoding="utf-8")
            saida = resultado.stdout + resultado.stderr
            self.assertEqual(0, resultado.returncode, f"{nome}\n{saida}")
            self.assertNotIn("FAIL", saida, f"{nome}\n{saida}")
            nota = re.search(r"production-readiness score: (\d+)/100", saida)
            self.assertIsNotNone(nota, f"{nome}\n{saida}")
            self.assertGreaterEqual(int(nota.group(1)), 90, f"{nome}\n{saida}")
            if "WARN" in saida:
                self.assertIn(f"Aviso {nome}:", refinamentos)

    def test_C7_previews_e_leituras_foram_inspecionados(self):
        refinamentos = texto(FEATURE / "refinamentos.md")
        for slug, direcao in zip(SLUGS, DIRECOES):
            self.assertTrue((FEATURE / f"preview-{slug}.html").is_file(), slug)
            bloco = secao(refinamentos, f"### {direcao}")
            for resultado in [
                "Escala 64/32/16: PASS",
                "Preto e invertido: PASS",
                "Espelho: PASS",
                "Rotação 90/180: PASS",
                "Desfoque: PASS",
                "Leitura indevida: nenhuma",
            ]:
                self.assertIn(resultado, bloco, f"{direcao}: {resultado}")

    def test_C8_duas_iteracoes_corrigem_defeito_observado(self):
        refinamentos = texto(FEATURE / "refinamentos.md")
        for slug, direcao in zip(SLUGS, DIRECOES):
            for parte in ["symbol", "lockup"]:
                v1 = CONCEITOS / f"{slug}-{parte}-v1.svg"
                v2 = CONCEITOS / f"{slug}-{parte}-v2.svg"
                self.assertTrue(v1.is_file(), v1.name)
                self.assertTrue(v2.is_file(), v2.name)
                self.assertNotEqual(hashlib.sha256(v1.read_bytes()).digest(), hashlib.sha256(v2.read_bytes()).digest(), f"{slug}-{parte}")
            bloco = secao(refinamentos, f"### {direcao}")
            self.assertRegex(bloco, r"Defeito v1: .+")
            self.assertRegex(bloco, r"Correção v2: .+")

    def test_C9_prateleira_contem_sete_marcas_e_familiaridade(self):
        prateleira = texto(FEATURE / "prateleira.html").lower()
        for nome in ["fonte-embutida", "manicula", "fio-procedencia", "marca-atual", "khan_academy", "zendesk", "intercom"]:
            self.assertIn(nome, prateleira)
        refinamentos = texto(FEATURE / "refinamentos.md")
        self.assertIn("Geometria externa reutilizada: nenhuma", refinamentos)
        for direcao in DIRECOES:
            self.assertIn("Familiaridade:", secao(refinamentos, f"### {direcao}"))

    def test_C10_seis_png_512_e_inspecao_registrada(self):
        refinamentos = texto(FEATURE / "refinamentos.md")
        for nome in FINAIS:
            png = FEATURE / "renders" / nome.replace(".svg", ".png")
            self.assertTrue(png.is_file(), png.name)
            self.assertEqual((512, 512), dimensoes_png(png), png.name)
            self.assertIn(f"Inspeção 512: {png.name} PASS", refinamentos)
        for slug in SLUGS:
            self.assertIn(f"Preview: preview-{slug}.html PASS", refinamentos)

    def test_C11_prancha_tem_tres_colunas_e_oito_itens(self):
        png = FEATURE / "conceitos.png"
        svg = FEATURE / "conceitos.svg"
        self.assertTrue(png.is_file())
        self.assertTrue(svg.is_file())
        self.assertGreaterEqual(dimensoes_png(png)[0], 1200)
        conteudo = texto(svg)
        for nome, tipo in [
            ("Fonte embutida", "wordmark-led"),
            ("Manícula", "pictorial"),
            ("Fio de procedência", "abstract"),
        ]:
            self.assertIn(nome, conteudo)
            self.assertIn(tipo, conteudo)
        self.assertEqual(3, conteudo.count(">64px<"))
        self.assertEqual(3, conteudo.count(">32px<"))
        self.assertEqual(3, conteudo.count(">16px<"))
        self.assertEqual(3, conteudo.count("Risco:"))
        self.assertEqual(3, len(re.findall(r"<rect x=\"[\d.]+\" y=\"150\"", conteudo)))

    def test_C12_recomendacao_cobre_brief_riscos_e_limite(self):
        recomendacao = texto(FEATURE / "recomendacao.md")
        self.assertEqual(1, len(re.findall(r"^## Recomendada:", recomendacao, re.M)))
        for adjetivo in ["claro", "confiável", "humano", "inteligente", "inesperado"]:
            self.assertIn(adjetivo, recomendacao.lower())
        self.assertEqual(3, len(re.findall(r"^Risco de .+: ", recomendacao, re.M)))
        self.assertIn("não garante exclusividade jurídica", recomendacao.lower())
        self.assertIn("não pode prometer a “melhor logo de todas”", recomendacao.lower())

    def test_C13_producao_permanece_intacta_e_nao_ha_kit(self):
        resultado = subprocess.run(["git", "status", "--porcelain"], cwd=RAIZ, capture_output=True, text=True, encoding="utf-8")
        self.assertEqual(0, resultado.returncode, resultado.stderr)
        producao = ("web/", "Esclari.Api/", "Esclari.Api.Tests/")
        alterados = []
        for linha in resultado.stdout.splitlines():
            caminho = linha[3:].replace("\\", "/")
            if caminho.startswith(producao):
                alterados.append(caminho)
        self.assertEqual([], alterados)
        diff = subprocess.run(["git", "diff", "--name-only", "108f95e19455f5a7ac0cc92d94bc969aa8c7b8dc", "--", *producao], cwd=RAIZ, capture_output=True, text=True, encoding="utf-8")
        self.assertEqual("", diff.stdout.strip())
        proibidos = [p.name for p in FEATURE.rglob("*") if re.search(r"(?:^|[-_])(final|color|kit)(?:[-_.]|$)", p.name, re.I)]
        self.assertEqual([], proibidos)

    def test_C14_recomendacao_termina_no_checkpoint(self):
        recomendacao = texto(FEATURE / "recomendacao.md").strip()
        self.assertTrue(recomendacao.endswith("Qual direção você escolhe, ou o que deseja revisar nas três?"))
        for frase in ["kit final está pronto", "aplicada no produto", "marca substituída"]:
            self.assertNotIn(frase, recomendacao.lower())


if __name__ == "__main__":
    unittest.main(verbosity=2)
