"""Small fail-closed reader for ordinary public methods in one global C# class.

This is deliberately not a C# parser. Unsupported declarations require a human
or the existing DLL collector. Bodies are compared at file granularity only.
"""
import re


TOKEN = re.compile(
    r'\s+|//[^\n]*|/\*[\s\S]*?\*/|"(?:\\[^\r\n]|[^"\\\r\n])*"'
    r'|\'(?:\\.|[^\'\\\r\n])*\'|[A-Za-z_][A-Za-z_0-9]*|\d[\w.]*'
    r'|>>=|<<=|\?\?=|=>|==|!=|>=|<=|\+\+|--|&&|\|\||\+=|-=|\*=|/=|%=|\?\?|\?\.|::|<<|>>|&=|\|=|\^=|[^\s]'
)
IDENT = r'[A-Za-z_][A-Za-z_0-9]*'
TYPE = IDENT + r'(?:\.' + IDENT + r')*'
ALIASES = dict(zip(
    ('bool byte sbyte short ushort int uint long ulong float double decimal char string object').split(),
    ('Boolean Byte SByte Int16 UInt16 Int32 UInt32 Int64 UInt64 Single Double Decimal Char String Object').split(),
))


def code_tokens(source: str) -> list[str]:
    if '"""' in source:
        raise ValueError('raw string syntax is unsupported')
    tokens = []
    for m in TOKEN.finditer(source):
        token = m.group()
        if token.isspace() or token.startswith(('//', '/*')):
            continue
        if token == '$' or (token == '@' and source[m.end():m.end()+1] == '"'):
            raise ValueError('interpolated/verbatim string syntax is unsupported')
        if token in ('"', "'") or token == '#' or token in ('/*', '*/'):
            raise ValueError('unterminated literal or preprocessor syntax')
        tokens.append(token)
    # Catch unterminated block comments which otherwise become '/' and '*'.
    if any(tokens[i:i+2] == ['/', '*'] for i in range(len(tokens))):
        raise ValueError('unterminated block comment')
    return tokens


def _type(text: str) -> str:
    return 'System.' + ALIASES[text] if text in ALIASES else text


def method_signatures(source: str, owner: str, name: str, is_static: bool) -> list[str]:
    tokens = code_tokens(source)
    declarations = []
    nesting = 0
    for i, token in enumerate(tokens):
        if token == 'class' and nesting == 0:
            declarations.append(i)
        nesting += (token == '{') - (token == '}')
    if len(declarations) != 1 or tokens[declarations[0] + 1] != owner or 'namespace' in tokens:
        raise ValueError('expected one global class matching configured owner')
    if tokens[declarations[0] + 2] not in (':', '{'):
        raise ValueError('generic class syntax is unsupported')
    start = tokens.index('{', declarations[0])
    depth = 0
    boundary = start + 1
    signatures = []
    for i in range(start, len(tokens)):
        token = tokens[i]
        if depth == 1 and token == name:
            if i + 1 >= len(tokens) or tokens[i + 1] != '(':
                raise ValueError('unsupported watched member declaration')
            prefix = ' '.join(tokens[boundary:i])
            match = re.fullmatch(r'(public|private|protected|internal) ((?:(?:static|virtual|override|sealed|new|abstract|extern) )*)(' + TYPE.replace(r'\.', r' \. ') + ')', prefix)
            if not match:
                raise ValueError('unsupported watched method prefix')
            end = tokens.index(')', i + 2)
            params = ' '.join(tokens[i + 2:end])
            if '(' in params or end + 1 >= len(tokens) or tokens[end + 1] not in ('{', '=>', ';'):
                raise ValueError('unsupported watched parameter syntax')
            types = []
            for param in params.split(',') if params else []:
                p = re.fullmatch(r'\s*(' + TYPE.replace(r'\.', r' \. ') + r') ' + IDENT + r'(?: = [^=,(){}]+)?\s*', param)
                if not p:
                    raise ValueError('only ordinary by-value parameters are supported')
                types.append(_type(p.group(1).replace(' ', '')))
            if match.group(1) == 'public' and ('static' in match.group(2).split()) == is_static:
                signatures.append(_type(match.group(3).replace(' ', '')) + '(' + ','.join(types) + ')')
        if token == '{':
            depth += 1
        elif token == '}':
            depth -= 1
            if depth < 0:
                raise ValueError('unbalanced class braces')
        if depth == 1 and token in ('{', '}', ';'):
            boundary = i + 1
    if depth != 0 or tokens[-1] != '}':
        raise ValueError('unbalanced class braces')
    return sorted(set(signatures))
