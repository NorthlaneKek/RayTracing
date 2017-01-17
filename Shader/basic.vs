#version 130
uniform float aspect;
uniform vec3 campos;
in vec3 vPosition; //¬ходные переменные vPosition - позци€ вершины
in vec3 camera;

out vec3 origion, direction;


void main() 
{ 
	gl_Position = vec4(vPosition, 1.0);
	direction = normalize(vec3(vPosition.x*aspect, vPosition.y, -1.0));
	origion = campos;
	//origion = camera;
}