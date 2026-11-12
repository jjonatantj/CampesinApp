# CampesinApp 

**Conectando el Campo Colombiano con la Ciudad sin Intermediarios**

## Propósito y Visión
CampesinApp nace con el propósito de eliminar la intermediación excesiva en la cadena de comercialización agrícola. El sistema garantiza precios justos para los productores y competitivos para los compradores mayoristas (empresas, restaurantes, plazas de mercado o consumidores independientes) mediante un canal directo de venta y gestión logística de transporte. 

Nuestra visión es convertirnos en la plataforma líder de comercialización agrícola directa en Cundinamarca para el año 2030, promoviendo la inclusión digital del sector rural.

## ¿Qué puedes hacer en CampesinApp?
La plataforma está diseñada para satisfacer las necesidades de diferentes actores mediante módulos específicos:

*   **Campesinos:** Pueden consultar el catálogo de demandas, responder a solicitudes ofertando sus productos agrícolas y recibir pagos seguros basados en precios justos.
*   **Compradores:** Tienen la capacidad de publicar demandas de productos con cantidades en kilogramos, asignar puntos de entrega, pagar a través de la pasarela y calificar el servicio recibido.
*   **Transportadores:** Pueden visualizar sus viajes asignados, reportar su ubicación en tiempo real mediante hitos de ruta (origen, cada 2 horas, acopio, destino) y confirmar la entrega.
*   **Administradores:** Gestionan la plataforma importando datos estadísticos del DANE, asignan vehículos a los pedidos, y acceden al historial global de transacciones y logs del aplicativo.

## Características Principales y Alcance
*   **Modelo Estadístico:** El aplicativo calcula precios sugeridos dinámicos integrando precios mayoristas diarios del DANE, datos históricos de 15 días y costos logísticos (fletes, combustible, desgaste, peajes).
*   **Gestión de Pagos:** El sistema retiene el dinero de la transacción de forma segura y solo lo distribuye automáticamente (al campesino, al transportador y la comisión de la app) cuando ambas partes validan la entrega exitosa y sin novedades.
*   **Mapas y Puntos de Acopio:** Integración con servicios de mapas para registrar y ubicar geográficamente los puntos de entrega y recepción.
*   **Arquitectura Segura:** Es un sistema web responsive optimizado para el navegador Chrome, con bases de datos relacionales (SQLServer) y encriptación no obsoleta para datos sensibles.
*   **Alcance (MVP):** Este repositorio contiene la implementación del 60% de las funcionalidades totales proyectadas para el periodo lectivo.

## Arquitectura y Tecnologías (Stack Tecnológico)
El proyecto está desarrollado bajo una arquitectura monolítica utilizando las siguientes tecnologías:
*   **Backend & Web API:** C# con ASP.NET Core MVC.
*   **Base de Datos:** SQL Server.
*   **Frontend (Plataforma Web):** HTML, CSS, JavaScript.
*   **Integraciones Adicionales:** API de mapas de Google Maps y pasarela de pagos (Openpay).
*   **Control de Versiones:** Git & GitHub.

## Instalación y Configuración Local

Si deseas ejecutar este proyecto en tu entorno local para evaluación académica, sigue estos pasos:

### Prerrequisitos
1.  Tener instalado [.NET 8.0 SDK]
2.  Tener instalado [SQL Server]
3.  Un IDE compatible como Visual Studio.

## Integrantes del Equipo
*   Carol Tatiana Manrique Galvis
*   Paula Alejandra Moncada Castblanco
*   Jaider Jonatan Tinoco Jimenez
