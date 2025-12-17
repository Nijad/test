.386
.model flat, stdcall
option casemap :none

include \\masm32\\include\\windows.inc
include \\masm32\\include\\kernel32.inc
include \\masm32\\include\\masm32.inc
includelib \\masm32\\lib\\kernel32.lib
includelib \\masm32\\lib\\masm32.lib

.data
; Struct: A
; Struct: B

.code
start:
call main
invoke ExitProcess, 0


main PROC
push ebp
mov ebp, esp
sub esp, 4
; Expression: obj.a=1
; Complex assignment: obj = ...
; Expression: 1
mov eax, 1
mov [ebp-4], eax
; Expression: obj.b=2
; Complex assignment: obj = ...
; Expression: 2
mov eax, 2
mov [ebp-4], eax
; Expression: obj.a+obj.b
; Integer operation: +
; Expression: obj.a
; Struct member access: a
mov eax, a
push eax
; Expression: obj.b
; Struct member access: b
mov eax, b
pop ebx
add eax, ebx
mov esp, ebp
pop ebp
ret
main ENDP
end start
