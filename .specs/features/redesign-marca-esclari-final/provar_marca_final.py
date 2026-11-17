import re
import struct
import subprocess
import sys
import unittest
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[3]
FEATURE = Path(__file__).resolve().parent
PUBLICA = RAIZ / "web/public/marca"
KIT = FEATURE / "kit"
MASTERS = ["simbolo.svg", "simbolo-pequeno.svg", "palavra-marca.svg", "logo-horizontal.svg", "logo-horizontal-escuro.svg", "logo-vertical.svg", "logo-preto.svg", "logo-branco.svg"]
PNGS = {
    "simbolo-256.png": (256, 256),
    "simbolo-512.png": (512, 512),
    "simbolo-1200.png": (1200, 1200),
    "simbolo-pequeno-256.png": (256, 256),
    "simbolo-pequeno-512.png": (512, 512),
    "palavra-marca-512.png": (512, 256),
    "palavra-marca-1200.png": (1200, 400),
    "logo-horizontal-512.png": (512, 256),
    "logo-horizontal-1200.png": (1200, 400),
    "logo-vertical-512.png": (512, 512),
    "logo-vertical-1200.png": (1200, 1200),
    "logo-preto-1200.png": (1200, 400),
    "logo-branco-1200.png": (1200, 400),
}


def dimensoes(caminho):
    dados = caminho.read_bytes()[:24]
    if dados[:8] != b"\x89PNG\r\n\x1a\n":
        raise AssertionError(f"{caminho} não é PNG")
    return struct.unpack(">II", dados[16:24])


class MarcaFinalTests(unittest.TestCase):
    def test_C04_auditoria_e_escala(self):
        auditor = Path.home() / ".kiro/skills/logo-design/scripts/svg_audit.py"
        for nome in MASTERS:
            resultado = subprocess.run([sys.executable, str(auditor), str(PUBLICA / nome)], capture_output=True, text=True, encoding="utf-8")
            saida = resultado.stdout + resultado.stderr
            self.assertEqual(0, resultado.returncode, f"{nome}\n{saida}")
            self.assertNotIn("FAIL", saida, f"{nome}\n{saida}")
            self.assertNotIn("WARN", saida, f"{nome}\n{saida}")
            nota = re.search(r"production-readiness score: (\d+)/100", saida)
            self.assertIsNotNone(nota, f"{nome}\n{saida}")
            self.assertGreaterEqual(int(nota.group(1)), 95, f"{nome}\n{saida}")
        folha = (FEATURE / "teste-tamanhos.svg").read_text(encoding="utf-8")
        self.assertTrue((FEATURE / "teste-tamanhos.png").is_file())
        for tamanho in ["16px", "24px", "32px", "64px", "256px"]:
            self.assertIn(tamanho, folha)
        self.assertIn("preto sobre branco", folha)
        self.assertIn("branco sobre preto", folha)

    def test_C15_kit_de_masters_e_pngs(self):
        svg = KIT / "svg"
        png = KIT / "png"
        self.assertEqual(sorted(MASTERS), sorted(p.name for p in svg.glob("*.svg")))
        for nome in MASTERS:
            self.assertEqual((PUBLICA / nome).read_bytes(), (svg / nome).read_bytes(), nome)
        self.assertEqual(sorted(PNGS), sorted(p.name for p in png.glob("*.png")))
        for nome, tamanho in PNGS.items():
            self.assertEqual(tamanho, dimensoes(png / nome), nome)

    def test_C16_seis_aplicacoes(self):
        html = (FEATURE / "apresentacao.html").read_text(encoding="utf-8")
        self.assertTrue((FEATURE / "apresentacao.png").is_file())
        self.assertGreaterEqual(dimensoes(FEATURE / "apresentacao.png")[0], 1200)
        aplicacoes = ["Aba do navegador", "Ícone PWA", "Cabeçalho de e-mail", "Capa de apresentação", "Instituição Horizonte", "Atendimento por Esclari"]
        for aplicacao in aplicacoes:
            self.assertEqual(1, html.count(aplicacao), aplicacao)
        self.assertEqual(6, len(re.findall(r'class="[^"]*\bmockup\b[^"]*"', html)))


if __name__ == "__main__":
    unittest.main(verbosity=2)
