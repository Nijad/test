.386
.model flat, stdcall
option casemap :none

include \\masm32\\include\\windows.inc
include \\masm32\\include\\kernel32.inc
include \\masm32\\include\\masm32.inc
includelib \\masm32\\lib\\kernel32.lib
includelib \\masm32\\lib\\masm32.lib

.data
; Struct: Config
version DWORD 0

.code
start:
call main
invoke ExitProcess, 0


main PROC
push ebp
mov ebp, esp
; Expression: Config.version=5
; Complex assignment: Config = ...
; Expression: 5
mov eax, 5
mov Config, eax
; Expression: Config.version
; Struct member access: Config.version
mov eax, version
mov esp, ebp
pop ebp
ret
main ENDP
end start
