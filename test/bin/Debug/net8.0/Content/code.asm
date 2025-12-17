.386
.model flat, stdcall
option casemap :none

include \masm32\include\windows.inc
include \masm32\include\kernel32.inc
include \masm32\include\masm32.inc
includelib \masm32\lib\kernel32.lib
includelib \masm32\lib\masm32.lib

.data
; Struct: Point
; Struct: Circle
PI REAL8 3.14159

.code
start:
call main
invoke ExitProcess, 0


area PROC
push ebp
mov ebp, esp
; Parameter: c at [ebp+8]
; Expression: Circle.PI*c.radius*c.radius
; Floating point operation: *
; Expression: Circle.PI*c.radius
; Floating point operation: *
; Expression: Circle.PI
; Struct member access: Circle.PI
fld PI
; Expression: c.radius
; Struct member access: c.radius
mov esi, [ebp+8]
fld qword ptr [esi+8]
fxch st(1)
fmulp st(1), st(0)
; Expression: c.radius
; Struct member access: c.radius
mov esi, [ebp+8]
fld qword ptr [esi+8]
fxch st(1)
fmulp st(1), st(0)
mov esp, ebp
pop ebp
ret
area ENDP

main PROC
push ebp
mov ebp, esp
sub esp, 24
; Expression: c.x=3
; Complex assignment: c = ...
; Expression: 3
mov eax, 3
mov [ebp-8], eax
; Expression: c.y=4
; Complex assignment: c = ...
; Expression: 4
mov eax, 4
mov [ebp-8], eax
; Expression: c.radius=5.0
; Complex assignment: c = ...
; Expression: 5.0
push __float64__(5.0)
fld qword ptr [esp]
add esp, 8
mov [ebp-8], eax
; Expression: area(c)
; Call function: area
lea eax, [ebp-8]
push eax
call area
add esp, 4
fstp qword ptr [ebp-24]
; Expression: a
; Load variable: a
fld qword ptr [ebp-24]
mov esp, ebp
pop ebp
ret
main ENDP
end start
