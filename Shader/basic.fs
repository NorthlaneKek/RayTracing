#version 130
const int raytraceDepth =  3; //Глубина следа луча
in vec4 color;
in vec3 origion, direction;
out vec4 outputColor;

struct Ray 
{
	vec3 origion; //Позиция из которой испускается луч
	vec3 direction; //нормализованное направление луча
};
struct Sphere
{
	vec3 center;  //Центр
	float radius;  //Радиус
	vec3 col;  //Цвет
};
struct Plane  //Плоскость
{
	vec3 point;  //Точка
	vec3 norm;  //Нормаль
	vec3 col; //Цвет
};


struct Intersection  //Пересечение
{
    float t;
    vec3 point;     // hit point
    vec3 norm;     // normal
    int hit;
    vec3 col;
};



//Проверка пересечения луча со сферой 
//Если пересекаются то идет пересчет цвета
void shpere_intersect(Sphere s,  Ray ray, inout Intersection isect)
{

 //µ = -(d . ∂p) ± √{ (d . ∂p)^2 - (|∂p|^2 - r^2)

    vec3 rs = ray.origion - s.center;							//расстояние от начала луча до центра сферы
    float B = dot(rs, ray.direction);  // (d . ∂p)				// dot - скалярное произведение
    float C = dot(rs, rs) - (s.radius * s.radius);  // |∂p|^2 - r^2
    float D = B * B - C;  

    if (D > 0.0)
    {
		float t = -B - sqrt(D);   //Луч входа
		if ( (t > 0.0) && (t < isect.t) )
		{
			isect.t = t;
			isect.hit = 1;

			// calculate normal.
			vec3 p = vec3(ray.origion.x + ray.direction.x * t,
						  ray.origion.y + ray.direction.y * t,
						  ray.origion.z + ray.direction.z * t);
			vec3 n = p - s.center;
			n = normalize(n);
			isect.norm = n;
			isect.point = p;
			isect.col = s.col;                 //яркость отражения сфер
		}
	}
}

//Пересечение с плоскостью
void plane_intersect(Plane pl, Ray ray, inout Intersection isect)
{
	float d = -dot(pl.point, pl.norm);
	float v = dot(ray.direction, pl.norm);

	if (abs(v) < 1.0e-6)
		return; // the plane is parallel to the ray.

    float t = -(dot(ray.origion, pl.norm) + d) / v;

    if ( (t > 0.0) && (t < isect.t) )
    {
		isect.hit = 1;
		isect.t   = t;
		isect.norm   = pl.norm;

		vec3 p = vec3(ray.origion.x + t * ray.direction.x,
					  ray.origion.y + t * ray.direction.y,
					  ray.origion.z + t * ray.direction.z);
		isect.point = p;
		float offset = 0.2;
		vec3 dp = p + offset;
    if ((mod(dp.x, 1.0) > 0.5 && mod(dp.z, 1.0) > 0.5) ||  (mod(dp.x, 1.0) < 0.5 && mod(dp.z, 1.0) < 0.5))
			isect.col = pl.col;
		else
			isect.col = pl.col*2;
	}
}




Sphere sphere[3];
Plane plane;
Plane plane2;
Plane plane3;

void Intersect(Ray r, inout Intersection i)  //Ближайшее пересечение с плоскостью или сферой
{
	for (int c = 0; c < 3; c++)
	{
		shpere_intersect(sphere[c], r, i);
	}
	plane_intersect(plane, r, i);
}

int seed = 0;


vec3 computeLightShadow(in Intersection isect)  //Вычисление тени объектов
{
	int i, j;
    int ntheta = 16;
    int nphi   = 16;
    float eps  = 0.0001;

    // Слегка переместить луч в направлении начала направления луча, чтобы избежать численных проблем.
    vec3 p = vec3(isect.point.x + eps * isect.norm.x,
                  isect.point.y + eps * isect.norm.y,
                  isect.point.z + eps * isect.norm.z);

	vec3 lightPoint = vec3(5,5,8);
    Ray ray;
	ray.origion = p;
	ray.direction = normalize(lightPoint - p);

	Intersection lisect;
	lisect.hit = 0;
	lisect.t = 1.0e+30;
	lisect.norm = lisect.point = lisect.col = vec3(0, 0, 0);
	Intersect(ray, lisect);




	if (lisect.hit != 0)
		return vec3(0.0,0.0,0.0);							//Цвет тени
	else
	{
		float shade = max(0.0, dot(isect.norm, ray.direction));
		shade = pow(shade,3.0) + shade * 0.5;
		return vec3(shade,shade,shade);
	}
	
}

void main()
{
	//Устанавливаем значения для сфер (центр,радиус,цвет)
	sphere[0].center   = vec3(0.7, -0.3, -1.0); // Левая - красная
	sphere[0].radius   = 0.2;
	sphere[0].col = vec3(0.7,0.2,0.2);
	sphere[1].center   = vec3(0, 0.0, -2.0); // Центральная - зеленая
	sphere[1].radius   = 0.5;
	sphere[1].col = vec3(0.2,0.7,0.2);
	sphere[2].center   = vec3(1.5, 0.0, -1.2); //Правая - синяя
	sphere[2].radius   = 0.5;
	sphere[2].col = vec3(0.2,0.2,0.7);
	plane.point = vec3(0,-0.5, 0);  //Установка поля (Расположение,нормаль,цвет)
	plane.norm = vec3(0, 1.0, 0);
	plane.col = vec3(0.5,1, 1);

	// seed = int(mod(direction.x * direction.y * 4525434.0, 65536.0));
	




	Ray r;
	r.origion = origion;
	r.direction = normalize(direction);
	vec4 col = vec4(0,0,0,1);				//(r,g,b,a), a - компонент прозрачности
	float eps  = 0.0001;                    //Отражение
	vec3 bcol = vec3(1,1,1);				//Фоновый цвет

	for (int j = 0; j < raytraceDepth; j++)
	{
		Intersection i;
		i.hit = 0;
		i.t = 1.0e+30;
		i.norm = i.point = i.col = vec3(0, 0, 0);
			
		Intersect(r, i);
		if (i.hit != 0)
		{
			col.rgb += bcol * i.col * computeLightShadow(i);
			bcol *= i.col;
		}
		else
		{
			break;
		}
				
		r.origion = vec3(i.point.x + eps * i.norm.x,
					 i.point.y + eps * i.norm.y,
					 i.point.z + eps * i.norm.z);
		r.direction = reflect(r.direction, vec3(i.norm.x, i.norm.y, i.norm.z));
	}
	 outputColor= col;
}

