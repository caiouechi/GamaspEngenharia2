/* =============================================================
   GAMA SP ENGENHARIA — interações do site
   Sem dependências externas.
   ============================================================= */
(function () {
    'use strict';

    var $ = function (sel, ctx) { return (ctx || document).querySelector(sel); };
    var $$ = function (sel, ctx) { return Array.prototype.slice.call((ctx || document).querySelectorAll(sel)); };
    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    /* ---------------------------------------------------------
       Cabeçalho fixo — encolhe ao rolar
       --------------------------------------------------------- */
    function initHeader() {
        var header = $('.site-header');
        if (!header) return;

        var ticking = false;
        function update() {
            header.classList.toggle('is-stuck', window.scrollY > 24);
            ticking = false;
        }
        window.addEventListener('scroll', function () {
            if (!ticking) { window.requestAnimationFrame(update); ticking = true; }
        }, { passive: true });
        update();
    }

    /* ---------------------------------------------------------
       Menu móvel
       --------------------------------------------------------- */
    function initMobileNav() {
        var burger = $('.burger');
        var menu = $('.mobile-nav');
        if (!burger || !menu) return;

        function setOpen(open) {
            burger.setAttribute('aria-expanded', open ? 'true' : 'false');
            menu.classList.toggle('is-open', open);
            document.body.classList.toggle('is-locked', open);
        }

        burger.addEventListener('click', function () {
            setOpen(burger.getAttribute('aria-expanded') !== 'true');
        });

        $$('a', menu).forEach(function (a) {
            a.addEventListener('click', function () { setOpen(false); });
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') setOpen(false);
        });
    }

    /* ---------------------------------------------------------
       Revelar elementos ao rolar
       --------------------------------------------------------- */
    function initReveal() {
        var items = $$('.reveal');
        if (!items.length) return;

        if (reduceMotion || !('IntersectionObserver' in window)) {
            items.forEach(function (el) { el.classList.add('is-in'); });
            return;
        }

        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-in');
                    io.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -8% 0px' });

        items.forEach(function (el) { io.observe(el); });
    }

    /* ---------------------------------------------------------
       Contadores animados
       --------------------------------------------------------- */
    function initCounters() {
        var nodes = $$('[data-count]');
        if (!nodes.length) return;

        function run(el) {
            var target = parseFloat(el.getAttribute('data-count'));
            if (isNaN(target)) return;
            if (reduceMotion) { el.textContent = String(target); return; }

            var dur = 1500, start = null;
            function frame(ts) {
                if (start === null) start = ts;
                var p = Math.min((ts - start) / dur, 1);
                // easeOutExpo
                var eased = p === 1 ? 1 : 1 - Math.pow(2, -10 * p);
                el.textContent = String(Math.round(target * eased));
                if (p < 1) window.requestAnimationFrame(frame);
            }
            window.requestAnimationFrame(frame);
        }

        if (!('IntersectionObserver' in window)) { nodes.forEach(run); return; }

        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) { run(entry.target); io.unobserve(entry.target); }
            });
        }, { threshold: 0.5 });

        nodes.forEach(function (el) { io.observe(el); });
    }

    /* ---------------------------------------------------------
       Perguntas frequentes (acordeão)
       --------------------------------------------------------- */
    function initFaq() {
        $$('.faq-q').forEach(function (btn) {
            var panel = btn.nextElementSibling;
            if (!panel) return;

            btn.addEventListener('click', function () {
                var open = btn.getAttribute('aria-expanded') === 'true';

                // fecha os irmãos
                var group = btn.closest('.faq');
                if (group && !open) {
                    $$('.faq-q[aria-expanded="true"]', group).forEach(function (other) {
                        other.setAttribute('aria-expanded', 'false');
                        other.nextElementSibling.style.height = '0px';
                    });
                }

                btn.setAttribute('aria-expanded', open ? 'false' : 'true');
                panel.style.height = open ? '0px' : panel.firstElementChild.offsetHeight + 'px';
            });
        });
    }

    /* ---------------------------------------------------------
       Modais de serviço
       --------------------------------------------------------- */
    function initModals() {
        var lastFocus = null;

        function close(modal) {
            modal.classList.remove('is-open');
            document.body.classList.remove('is-locked');
            if (lastFocus) { lastFocus.focus(); lastFocus = null; }
        }

        $$('[data-modal-open]').forEach(function (trigger) {
            trigger.addEventListener('click', function () {
                var modal = document.getElementById(trigger.getAttribute('data-modal-open'));
                if (!modal) return;
                lastFocus = trigger;
                modal.classList.add('is-open');
                document.body.classList.add('is-locked');
                var closeBtn = $('.modal-close', modal);
                if (closeBtn) closeBtn.focus();
            });
        });

        $$('.modal').forEach(function (modal) {
            modal.addEventListener('click', function (e) {
                if (e.target === modal || e.target.closest('[data-modal-close]')) close(modal);
            });
        });

        document.addEventListener('keydown', function (e) {
            if (e.key !== 'Escape') return;
            var open = $('.modal.is-open');
            if (open) close(open);
        });
    }

    /* ---------------------------------------------------------
       Galeria: filtros + lightbox
       --------------------------------------------------------- */
    function initGallery() {
        // filtros
        var filters = $$('.filter-btn');
        var tiles = $$('.tile[data-cat]');

        filters.forEach(function (btn) {
            btn.addEventListener('click', function () {
                var cat = btn.getAttribute('data-filter');
                filters.forEach(function (b) { b.classList.toggle('is-active', b === btn); });
                tiles.forEach(function (tile) {
                    var show = cat === 'todos' || tile.getAttribute('data-cat') === cat;
                    tile.classList.toggle('is-hidden', !show);
                });
            });
        });

        // lightbox
        var box = $('.lightbox');
        if (!box) return;

        var img = $('.lightbox img');
        var cap = $('.lightbox figcaption');
        var current = -1;

        function visibleTiles() {
            return $$('.tile[data-full]').filter(function (t) { return !t.classList.contains('is-hidden'); });
        }

        function show(i) {
            var list = visibleTiles();
            if (!list.length) return;
            current = (i + list.length) % list.length;
            var tile = list[current];
            img.src = tile.getAttribute('data-full');
            img.alt = tile.getAttribute('data-caption') || '';
            cap.textContent = tile.getAttribute('data-caption') || '';
        }

        function open(i) {
            show(i);
            box.classList.add('is-open');
            document.body.classList.add('is-locked');
        }

        function close() {
            box.classList.remove('is-open');
            document.body.classList.remove('is-locked');
        }

        $$('.tile[data-full]').forEach(function (tile) {
            tile.addEventListener('click', function () {
                open(visibleTiles().indexOf(tile));
            });
        });

        $('.lb-close').addEventListener('click', close);
        $('.lb-prev').addEventListener('click', function (e) { e.stopPropagation(); show(current - 1); });
        $('.lb-next').addEventListener('click', function (e) { e.stopPropagation(); show(current + 1); });
        box.addEventListener('click', function (e) { if (e.target === box) close(); });

        document.addEventListener('keydown', function (e) {
            if (!box.classList.contains('is-open')) return;
            if (e.key === 'Escape') close();
            if (e.key === 'ArrowLeft') show(current - 1);
            if (e.key === 'ArrowRight') show(current + 1);
        });
    }

    /* ---------------------------------------------------------
       Mascaras de digitacao

       Corrigem o valor enquanto a pessoa escreve, para que ela
       nao consiga errar o formato sem perceber.
       --------------------------------------------------------- */
    var MASCARAS = {
        // (11) 91234-5678  /  (11) 1234-5678
        telefone: function (v) {
            var d = v.replace(/\D/g, '').slice(0, 11);
            if (!d) return '';
            if (d.length <= 2) return '(' + d;
            var corte = d.length > 10 ? 7 : 6;
            var meio = d.slice(2, corte);
            var fim = d.slice(corte);
            return '(' + d.slice(0, 2) + ') ' + meio + (fim ? '-' + fim : '');
        },

        // Letras, espaco, hifen e apostrofo. Sem numeros nem simbolos.
        nome: function (v) {
            return v
                .replace(/[0-9]/g, '')
                .replace(/[^\p{L}\s'\-]/gu, '')
                .replace(/\s{2,}/g, ' ')
                .replace(/^\s+/, '');
        },

        // Sem espacos, sempre minusculo, um unico arroba.
        email: function (v) {
            v = v.replace(/\s+/g, '').toLowerCase();
            var i = v.indexOf('@');
            if (i !== -1) v = v.slice(0, i + 1) + v.slice(i + 1).replace(/@/g, '');
            return v;
        }
    };

    /* ---------------------------------------------------------
       Regras de validacao (mensagem = erro; null = ok)
       --------------------------------------------------------- */
    var REGRAS = {
        nome: function (v, campo) {
            v = v.trim();
            if (!v) return 'Por favor, informe o seu nome.';
            if (v.length < 3) return 'Nome muito curto.';
            if (v.split(/\s+/).length < 2) return 'Informe o nome e o sobrenome.';
            return null;
        },
        email: function (v) {
            v = v.trim();
            if (!v) return 'Por favor, informe o seu e-mail.';
            if (!/^[^\s@]+@[^\s@]+\.[a-z]{2,}$/i.test(v)) return 'E-mail inválido. Exemplo: nome@empresa.com.br';
            if (/\.\./.test(v)) return 'E-mail inválido.';
            return null;
        },
        telefone: function (v) {
            var d = v.replace(/\D/g, '');
            if (!d) return null;                       // campo opcional
            if (d.length < 10) return 'Telefone incompleto. Use DDD + número.';
            if (d.length === 11 && d.charAt(2) !== '9') return 'Celular com 11 dígitos deve começar com 9 após o DDD.';
            if (/^(\d)\1+$/.test(d)) return 'Telefone inválido.';
            return null;
        },
        mensagem: function (v, campo) {
            v = v.trim();
            var min = parseInt(campo.getAttribute('data-min'), 10) || 15;
            if (!v) return 'Conte-nos o que você precisa.';
            if (v.length < min) return 'Descreva um pouco mais (mínimo ' + min + ' caracteres).';
            return null;
        }
    };

    /* ---------------------------------------------------------
       Formulário de contato
       --------------------------------------------------------- */
    function initForm() {
        var form = $('#form-contato');
        if (!form) return;

        var msg = $('#form-msg');
        var submit = $('button[type="submit"]', form);
        var rotulo = submit ? submit.querySelector('.btn-label') : null;
        var textoOriginal = rotulo ? rotulo.textContent : '';
        var campos = $$('[data-regra]', form);

        // --- máscaras ao digitar ---
        $$('[data-mask]', form).forEach(function (campo) {
            var fn = MASCARAS[campo.getAttribute('data-mask')];
            if (!fn) return;
            campo.addEventListener('input', function () {
                var antes = campo.value;
                var fim = campo.selectionEnd === antes.length;
                var depois = fn(antes);
                if (depois === antes) return;
                campo.value = depois;
                // se o cursor estava no fim, mantém no fim depois da máscara
                if (fim) { try { campo.setSelectionRange(depois.length, depois.length); } catch (e) {} }
            });
        });

        // --- contador de caracteres ---
        $$('[data-min]', form).forEach(function (campo) {
            var conta = campo.parentElement.querySelector('[data-conta]');
            if (!conta) return;
            var max = campo.getAttribute('maxlength') || '';
            var min = parseInt(campo.getAttribute('data-min'), 10) || 0;
            function atualiza() {
                var n = campo.value.trim().length;
                conta.textContent = n + (max ? ' / ' + max : '');
                conta.classList.toggle('is-low', n > 0 && n < min);
            }
            campo.addEventListener('input', atualiza);
            atualiza();
        });

        // --- validação por campo ---
        function erroDe(campo) {
            var regra = REGRAS[campo.getAttribute('data-regra')];
            return regra ? regra(campo.value, campo) : null;
        }

        function marca(campo, erro) {
            var caixa = campo.closest('.field');
            if (!caixa) return;
            var alvo = caixa.querySelector('.field-erro');
            caixa.classList.toggle('is-invalid', !!erro);
            caixa.classList.toggle('is-ok', !erro && campo.value.trim().length > 0);
            campo.setAttribute('aria-invalid', erro ? 'true' : 'false');
            if (alvo) alvo.textContent = erro || '';
        }

        campos.forEach(function (campo) {
            campo.addEventListener('blur', function () { marca(campo, erroDe(campo)); });
            // ao corrigir, o erro some na hora
            campo.addEventListener('input', function () {
                var caixa = campo.closest('.field');
                if (caixa && caixa.classList.contains('is-invalid') && !erroDe(campo)) marca(campo, null);
            });
        });

        function diz(texto, ok) {
            msg.textContent = texto;
            msg.className = 'form-msg is-shown ' + (ok ? 'is-ok' : 'is-err');
        }

        form.addEventListener('submit', function (e) {
            e.preventDefault();

            // valida tudo de uma vez e leva o foco ao primeiro problema
            var primeiro = null;
            campos.forEach(function (campo) {
                var erro = erroDe(campo);
                marca(campo, erro);
                if (erro && !primeiro) primeiro = campo;
            });

            if (primeiro) {
                diz('Confira os campos destacados acima.', false);
                primeiro.focus();
                return;
            }

            if (submit) { submit.disabled = true; if (rotulo) rotulo.textContent = 'Enviando…'; }
            msg.className = 'form-msg';

            var dados = new FormData(form);
            var params = new URLSearchParams();
            dados.forEach(function (v, k) { params.append(k, v.toString()); });

            fetch(form.getAttribute('action'), {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
                body: params.toString()
            })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    if (res && res.sucesso) {
                        diz(res.mensagem || 'Mensagem enviada! Retornaremos em breve.', true);
                        form.reset();
                        $$('.field', form).forEach(function (c) { c.classList.remove('is-ok', 'is-invalid'); });
                        $$('[data-conta]', form).forEach(function (c) { c.textContent = '0 / 1500'; });
                    } else {
                        diz((res && res.mensagem) || 'Não foi possível enviar agora. Fale conosco pelo WhatsApp.', false);
                    }
                })
                .catch(function () {
                    diz('Falha de conexão. Por favor, fale conosco pelo WhatsApp: (11) 94743-0418.', false);
                })
                .then(function () {
                    if (submit) { submit.disabled = false; if (rotulo) rotulo.textContent = textoOriginal; }
                });
        });
    }

    /* ---------------------------------------------------------
       Ano corrente no rodapé
       --------------------------------------------------------- */
    function initYear() {
        $$('[data-year]').forEach(function (el) { el.textContent = new Date().getFullYear(); });
    }


    /* ---------------------------------------------------------
       Seção 3D — camadas do projeto
       --------------------------------------------------------- */
    function initLayers() {
        var stage = $('.layers-stage');
        if (!stage) return;

        var camadas = $$('.layer', stage);
        var botoes = $$('.layers-legend button');
        if (!camadas.length) return;

        // altura de cada camada quando "aberta" (em px, no eixo Z)
        var ABERTO = 46;

        function aplicar(ativo) {
            camadas.forEach(function (c, i) {
                c.style.setProperty('--z', (i * ABERTO) + 'px');
                c.classList.toggle('is-on', i === ativo);
            });
            botoes.forEach(function (b, i) { b.classList.toggle('is-on', i === ativo); });
        }

        botoes.forEach(function (b, i) {
            b.addEventListener('mouseenter', function () { aplicar(i); });
            b.addEventListener('focus', function () { aplicar(i); });
            b.addEventListener('click', function () { aplicar(i); });
        });

        camadas.forEach(function (c, i) {
            c.addEventListener('mouseenter', function () { aplicar(i); });
        });

        aplicar(camadas.length - 1);

        // percorre as camadas sozinho enquanto a seção está visível
        if (reduceMotion || !('IntersectionObserver' in window)) return;

        var atual = camadas.length - 1, timer = null;

        function ciclo() {
            atual = (atual + 1) % camadas.length;
            aplicar(atual);
        }

        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting && !timer) {
                    timer = window.setInterval(ciclo, 2600);
                } else if (!entry.isIntersecting && timer) {
                    window.clearInterval(timer); timer = null;
                }
            });
        }, { threshold: 0.35 });

        io.observe(stage);

        stage.addEventListener('mouseenter', function () {
            if (timer) { window.clearInterval(timer); timer = null; }
        });
    }


    /* ---------------------------------------------------------
       Parallax e profundidade 3D conforme a rolagem

       Um único laço de rAF cuida de todos os elementos: lemos as
       posições em lote e só escrevemos transformações via custom
       properties, para não provocar reflow a cada quadro.
       --------------------------------------------------------- */
    function initScroll3D() {
        if (reduceMotion) { document.body.classList.add('no-scroll3d'); return; }

        var camadas = $$('[data-parallax]');
        var blocos = $$('.rise3d');
        if (!camadas.length && !blocos.length) return;

        var alvos = [];

        camadas.forEach(function (el) {
            alvos.push({
                el: el,
                tipo: 'parallax',
                forca: parseFloat(el.getAttribute('data-parallax')) || 12,
                prop: el.getAttribute('data-parallax-prop') || '--py'
            });
        });

        blocos.forEach(function (el) {
            alvos.push({
                el: el,
                tipo: 'rise',
                desloca: parseFloat(el.getAttribute('data-rise-ty')) || 34
            });
        });

        var ticking = false;

        function desenha() {
            ticking = false;
            var vh = window.innerHeight || 1;

            for (var i = 0; i < alvos.length; i++) {
                var a = alvos[i];
                var r = a.el.getBoundingClientRect();

                // fora de vista (com folga) não precisa de cálculo
                if (r.bottom < -vh * 0.4 || r.top > vh * 1.4) continue;

                if (a.tipo === 'parallax') {
                    // -1 quando o elemento está abaixo da tela, +1 quando acima
                    var centro = (r.top + r.height / 2 - vh / 2) / (vh / 2 + r.height / 2);
                    if (centro < -1.4) centro = -1.4;
                    if (centro > 1.4) centro = 1.4;
                    a.el.style.setProperty(a.prop, (centro * a.forca).toFixed(2) + 'px');
                } else {
                    // 0 quando o topo ainda está na base da tela, 1 quando assentado
                    var t = (vh - r.top) / (vh * 0.62);
                    if (t < 0) t = 0;
                    if (t > 1) t = 1;
                    // easeOutCubic
                    var e = 1 - Math.pow(1 - t, 3);
                    var resto = 1 - e;

                    a.el.style.setProperty('--ty', (a.desloca * resto).toFixed(1) + 'px');
                    a.el.style.setProperty('--op', (0.3 + 0.7 * e).toFixed(3));
                }
            }
        }

        function agenda() {
            if (!ticking) { ticking = true; window.requestAnimationFrame(desenha); }
        }

        window.addEventListener('scroll', agenda, { passive: true });
        window.addEventListener('resize', agenda);
        // Ao voltar de uma aba em segundo plano o rAF pendente pode ter ficado
        // preso; recalcula para o conteudo nunca aparecer no estado errado.
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) { ticking = false; agenda(); }
        });
        desenha();
    }

    /* ---------------------------------------------------------
       Inclina o palco das camadas conforme a rolagem
       --------------------------------------------------------- */
    function initLayersScroll() {
        var stage = $('.layers-stage');
        var layers = $('.layers');
        if (!stage || !layers || reduceMotion) return;

        var pausado = false;
        stage.addEventListener('mouseenter', function () { pausado = true; });
        stage.addEventListener('mouseleave', function () { pausado = false; });

        var ticking = false;

        function desenha() {
            ticking = false;
            if (pausado) return;

            var vh = window.innerHeight || 1;
            var r = stage.getBoundingClientRect();
            if (r.bottom < 0 || r.top > vh) return;

            // 0 -> 1 enquanto a seção atravessa a tela
            var t = (vh - r.top) / (vh + r.height);
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            var rx = 66 - t * 22;   // 66deg -> 44deg
            var rz = -40 + t * 16;  // -40deg -> -24deg
            layers.style.transform = 'rotateX(' + rx.toFixed(1) + 'deg) rotateZ(' + rz.toFixed(1) + 'deg)';
        }

        window.addEventListener('scroll', function () {
            if (!ticking) { ticking = true; window.requestAnimationFrame(desenha); }
        }, { passive: true });

        desenha();
    }


    /* ---------------------------------------------------------
       Painel de imagens com miniaturas

       Passar o mouse (ou focar/clicar) numa miniatura troca a foto
       grande. Sozinho, o painel avança de tempos em tempos; qualquer
       interação assume o controle e interrompe o avanço automático.
       --------------------------------------------------------- */
    function initPainel() {
        $$('.painel').forEach(function (painel) {
            var fotos = $$('.painel-foto', painel);
            var minis = $$('.mini', painel);
            var slides = $$('.painel-slide', painel);
            var barra = $('.painel-barra i', painel);
            if (fotos.length < 2 || minis.length !== fotos.length) return;

            var atual = 0;
            var timer = null;
            var visivel = false;
            var tomouControle = false;
            var INTERVALO = 6500;

            painel.style.setProperty('--painel-tempo', (INTERVALO / 1000) + 's');

            function mostra(i) {
                if (i === atual) return;
                atual = i;

                fotos.forEach(function (f, n) { f.classList.toggle('is-on', n === i); });
                minis.forEach(function (m, n) {
                    m.classList.toggle('is-on', n === i);
                    m.setAttribute('aria-selected', n === i ? 'true' : 'false');
                    m.tabIndex = n === i ? 0 : -1;
                });
                slides.forEach(function (l, n) { l.classList.toggle('is-on', n === i); });

                reiniciaBarra();
            }

            function reiniciaBarra() {
                if (!barra || reduceMotion) return;
                barra.classList.remove('is-running');
                // força o reinício da animação
                void barra.offsetWidth;
                if (visivel && !tomouControle) barra.classList.add('is-running');
            }

            function liga() {
                if (timer || tomouControle || reduceMotion) return;
                timer = window.setInterval(function () {
                    mostra((atual + 1) % fotos.length);
                }, INTERVALO);
                reiniciaBarra();
            }

            function desliga() {
                if (timer) { window.clearInterval(timer); timer = null; }
                if (barra) barra.classList.remove('is-running');
            }

            minis.forEach(function (mini, i) {
                function assume() {
                    tomouControle = true;
                    desliga();
                    mostra(i);
                }
                mini.addEventListener('mouseenter', assume);
                mini.addEventListener('focus', assume);
                mini.addEventListener('click', function (e) { e.preventDefault(); assume(); });
            });

            // Setas do teclado percorrem as miniaturas
            painel.addEventListener('keydown', function (e) {
                if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;
                if (minis.indexOf(document.activeElement) === -1) return;
                e.preventDefault();
                var proximo = e.key === 'ArrowRight'
                    ? (atual + 1) % minis.length
                    : (atual - 1 + minis.length) % minis.length;
                minis[proximo].focus();
            });

            // Só roda sozinho enquanto estiver na tela
            if ('IntersectionObserver' in window) {
                var io = new IntersectionObserver(function (entries) {
                    entries.forEach(function (entry) {
                        visivel = entry.isIntersecting;
                        if (visivel) { liga(); } else { desliga(); }
                    });
                }, { threshold: 0.35 });
                io.observe(painel);
            } else {
                visivel = true;
                liga();
            }
        });
    }

    /* --------------------------------------------------------- */
    function boot() {
        initHeader();
        initMobileNav();
        initReveal();
        initCounters();
        initFaq();
        initModals();
        initGallery();
        initForm();
        initYear();
        initLayers();
        initScroll3D();
        initLayersScroll();
        initPainel();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();
